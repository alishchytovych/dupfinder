using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DupFinder {
	public class ConsoleSpinner {
		bool Locked = false;
		private int Counter = 0;
		DateTime LastSpinnerCall = DateTime.Now;
		DateTime LastSpeedCall = DateTime.Now;
		int CallCounter = 0;
		char[] Symbol = { '|', '/', '-', '\\' };
		string Speed = "                  ";

		public ConsoleSpinner() {
			Console.CursorVisible = false;
		}

		public void Turn(double percent) {
			Interlocked.Increment(ref CallCounter);
			if (Locked)
				return;
			else
				Locked = true;
			
			DateTime now = DateTime.Now;

			char spinner = Symbol[Counter % Symbol.Length];
			string progress = percent.ToString("00.00%");

			if ((now - LastSpinnerCall).TotalMilliseconds > 100) { Counter++; LastSpinnerCall = now; }
			if ((now - LastSpeedCall).TotalMilliseconds > 1000) {
				Speed = " [" + ((double)CallCounter / (now - LastSpeedCall).Seconds).ToString("0") + " files/sec]";

				CallCounter = 0;
				LastSpeedCall = now;
			}
			//int x = Console.CursorLeft, y = Console.CursorTop;
			//Console.SetCursorPosition(0, 0);
			
			Console.Write($"{spinner}{progress}{Speed}   ");
			Console.SetCursorPosition(0, Console.CursorTop);
			//Console.SetCursorPosition(x, y);		

			Locked = false;
		}

		~ConsoleSpinner() {
			Console.CursorVisible = true;
		}
	}
}
