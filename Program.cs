using System;
using System.IO;
using System.Linq;
using Serilog;
using OpenCvSharp;
using System.Collections.Generic;
using OpenCvSharp.ImgHash;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DupFinder {
	class Program {
		static string[] Ext = { ".jpg", ".png" };
		static ConcurrentBag<File> Files = new ConcurrentBag<File>();
		private static int CVError(ErrorCode status, string funcName, string errMsg, string fileName, int line, IntPtr userdata) {
			//Log.Warning(errMsg);
			return -1;
		}

		static void Main(string[] args) {
			DateTime start = DateTime.Now;
			ConsoleSpinner spin = new ConsoleSpinner();
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console()
				.WriteTo.File("log.txt")
				.CreateLogger();
			//string dir = @"c:\Users\AL\Documents\Visual Studio 2017\Projects\DupFinder\test";
			string dir = @"B:\DiskE\BACKUP";
			Log.Information("Analyzing {0}...", dir);
			DirectoryInfo dirInfo = new DirectoryInfo(dir);

			var files = dirInfo.EnumerateDirectories()
					   .AsParallel()
					   .SelectMany(di => di.EnumerateFiles("*.*", SearchOption.AllDirectories))
					   .Where(x => Array.IndexOf(Ext, x.Extension.ToLowerInvariant()) >= 0)
					   .ToArray();

			TimeSpan duration = DateTime.Now - start;
			Log.Information("Found {0} file(s), duration {1} sec", files.Count(), duration.TotalSeconds);

			Log.Information("Calculating images' hashes...");
			CvErrorCallback cvErr = CVError;
			IntPtr zero = IntPtr.Zero;
			NativeMethods.redirectError(cvErr,zero, ref zero);
			Console.SetError(TextWriter.Null);

			int c = 0;
			Parallel.ForEach(files, f => {
				try {
					RadialVarianceHash h1 = RadialVarianceHash.Create();
					BlockMeanHash h2 = BlockMeanHash.Create();
					byte[] data = System.IO.File.ReadAllBytes(f.FullName);
					using (Mat mat = Mat.FromImageData(data, ImreadModes.Color)) {
						InputOutputArray h1o = new Mat();
						InputOutputArray h2o = new Mat();

						h1.Compute(mat, h1o);
						h2.Compute(mat, h2o);

						Files.Add(new File {
							FileInfo = f,
							RadialVarianceHash = h1o,
							BlockMeanHash = h2o,
						});
						spin.Turn((double)(c++) / (double)files.Count());
					}
				} catch (Exception ex) {
					if(ex.Source!="OpenCvSharp")
						Log.Error("Error with {0}:\n\t{1}", f.FullName, ex.Message);
				}
			});

			Log.Information("Finding duplicates...");
			RadialVarianceHash hh1 = RadialVarianceHash.Create();
			BlockMeanHash hh2 = BlockMeanHash.Create();
			ulong saveSize = 0;
			bool dupMsg = true;
			int total = Files.Count;
			int counter = 0;
			for (int x = 0; x < Files.Count; x++) {
				if (Files.ElementAt(x).Duplicate != true) {
					counter++;
					dupMsg = true;
					for (int y = x + 1; y < Files.Count; y++) {
						if (Files.ElementAt(y).Duplicate == true) continue;
						double compare1 = hh1.Compare(InputArray.Create(Files.ElementAt(x).RadialVarianceHash.GetMat()), InputArray.Create(Files.ElementAt(y).RadialVarianceHash.GetMat()));
						double compare2 = hh2.Compare(InputArray.Create(Files.ElementAt(x).BlockMeanHash.GetMat()), InputArray.Create(Files.ElementAt(y).BlockMeanHash.GetMat()));
						if (compare1 > 0.98 && compare2 < 3) {
							saveSize += (ulong)Files.ElementAt(x).FileInfo.Length;
							Files.ElementAt(y).Duplicate = true;
							total--;
							if (dupMsg) {
								Log.Information("Dups for {0}:", Files.ElementAt(x).FileInfo.FullName);
								dupMsg = false;
							}
							Log.Information("\t{0}", Files.ElementAt(y).FileInfo.FullName);
						}
						spin.Turn((double)counter / (double)total);
					}
				}
				spin.Turn((double)counter / (double)total);
			}
			Log.Information("Done, possible size save is {0} Mb", saveSize/1024/1024);
			Console.ReadKey();
		}
	}
}
