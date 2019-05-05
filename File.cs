using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.ImgHash;

namespace DupFinder {
	class File {
		public bool Duplicate = false;
		public FileInfo FileInfo;
		public InputOutputArray RadialVarianceHash;
		public InputOutputArray BlockMeanHash;
	}
}
