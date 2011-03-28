using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AnagramSharp
{
	public partial class MainWindow : Form
	{
		private readonly AnagramDawg _anagram;
		private readonly SharpDawg _dawg;
		private readonly BindingSource bindingAnagramsFound = new BindingSource();
		private readonly BindingSource bindingAnagramsPicked = new BindingSource();

		public MainWindow()
		{
			// Improved font.
			Font = SystemFonts.MessageBoxFont;

			InitializeComponent();

			_dawg = new SharpDawg();
			_anagram = new AnagramDawg(_dawg);
		}

		/// <summary>
		/// Return a string indicating how many words were found.
		/// </summary>
		/// <param name="count">The number of words found.</param>
		/// <returns>Correctly pluralized word string.</returns>
		private static string PluralPhrase(int count)
		{
			return count == 1 ? "1 word" : count + " words";
		}

		private void button1_Click(object sender, EventArgs e)
		{
			List<string> results = _anagram.FindForString(textBox1.Text);
			string chall = textBox1.Text;

			// Ok lets do couple of things here 
			dataGridView1.DataSource = bindingAnagramsFound;
			GetAnagramsFound(results);

			dataGridView2.DataSource = bindingAnagramsPicked;
			GetAnagramsPicked(results.ToArray(), chall);


			Dictionary<string, int> frequentWords = LoadFrequentWords();

			// LINQ sort words by length, and then by first letter.
			string[] byLength = (from item in results
								 where item.Length > 2
								 orderby item.Length descending
								 select item).ToArray();

			var valueList = new List<string>();

			var dict = new Dictionary<string, string>();

			foreach (string str in byLength)
			{
				int freq;
				if (frequentWords.TryGetValue(str.ToUpper(), out freq))
				{
					string mod = string.Format("word '{0}' found in freqlist with freq '{1}' ", str, freq);
					dict.Add(str, mod);
				}
				else
				{
					int val = ScrabbleValue(str.ToUpper());
					string mod = string.Format("word '{0}' not found so ordering by scrabble value '{1}'", str, val);
					dict.Add(str, mod);
				}
			}

			IOrderedEnumerable<KeyValuePair<string, string>> order = dict.OrderBy(r => r.Key.Length);

			foreach (var pair in order)
			{
				valueList.Add(pair.Value);
			}

			listBox1.DataSource = valueList;

			BuildMergedArray(byLength, chall);

			// Write the plural string to the status bar.
			toolStripStatusLabel1.Text = PluralPhrase(byLength.Length);
		}

		private void GetAnagramsFound(List<string> anagramsFound)
		{
			Dictionary<string, int> frequentWords = LoadFrequentWords();

			var dt = new DataTable();
			dt.Columns.Add("Word");
			dt.Columns.Add("Freq");
			dt.Columns.Add("S_Value");

			foreach (string wd in anagramsFound)
			{
				DataRow row = dt.NewRow();
				row["Word"] = wd;

				int freq;
				if (frequentWords.TryGetValue(wd.ToUpper(), out freq))
					row["Freq"] = freq;
				else
					row["Freq"] = 0;

				row["S_Value"] = ScrabbleValue(wd.ToUpper());

				dt.Rows.Add(row);
			}

			bindingAnagramsFound.DataSource = dt;
		}

		private void GetAnagramsPicked(string[] anagramsFound, string challenge)
		{
			Dictionary<string, int> freqList = LoadFrequentWords();

			var three = ConstructWordsTable(anagramsFound, 3, 13, freqList);
			var four = ConstructWordsTable(anagramsFound, 4, 8, freqList);
			var five = ConstructWordsTable(anagramsFound, 5, 6, freqList);
			var six = ConstructWordsTable(anagramsFound, 6, 4, freqList);
			//string cc = string.Format("{0} was the challenge", challenge);
			//var seven = new List<string> { cc };

			var seven = new DataTable();
			seven.Columns.Add("Word");
			seven.Columns.Add("Freq");
			seven.Columns.Add("S_Value");
			DataRow row = seven.NewRow();
			row["Word"] = challenge;
			row["Freq"] = 0;
			row["S_Value"] = ScrabbleValue(challenge.ToUpper());

			seven.Rows.Add(row);

			var master = new DataTable();
			master.Columns.Add("Word");
			master.Columns.Add("Freq");
			master.Columns.Add("S_Value");

			Merge(master, new DataTable[] { three, four, five, six, seven });
		}

		void Merge(DataTable masterDataTable, DataTable[] dataTables)
		{
			foreach (DataTable dt in dataTables)
			{
				foreach (DataRow dr in dt.Rows)
				{
					DataRow current = masterDataTable.NewRow();

					for (int i = 0; i < dt.Columns.Count; i++)
					{
						current[i] = dr[i];
					}
					masterDataTable.Rows.Add(current);
				}
			}

			bindingAnagramsPicked.DataSource = masterDataTable;
		}

		private DataTable ConstructWordsTable(string[] master, int wordLength, int maxObjectInArray,
											Dictionary<string, int> frequentWords)
		{
			var dt = new DataTable();
			dt.Columns.Add("Word");
			dt.Columns.Add("Freq");
			dt.Columns.Add("S_Value");


			List<string> items = master.Where(r => r.Length == wordLength).ToList();

			//var dict = new Dictionary<string, string>();
			foreach (string word in items)
			{
				DataRow row = dt.NewRow();
				row["Word"] = word;

				int freq;
				if (frequentWords.TryGetValue(word.ToUpper(), out freq))
					row["Freq"] = freq;
				else
					row["Freq"] = 0;

				row["S_Value"] = ScrabbleValue(word.ToUpper());

				dt.Rows.Add(row);
			}

			var retDt = new DataTable();
			retDt.Columns.Add("Word");
			retDt.Columns.Add("Freq");
			retDt.Columns.Add("S_Value");

			for (int i = 0; i < dt.Rows.Count; i++)
			{
				int freq = Int32.Parse(dt.Rows[i]["Freq"].ToString());
				if (freq > 0 && retDt.Rows.Count <= maxObjectInArray)
				{
					DataRow row = retDt.NewRow();
					var wd = dt.Rows[i]["Word"].ToString();
					row["Word"] = wd;
					row["Freq"] = dt.Rows[i]["Freq"].ToString();
					row["S_Value"] = ScrabbleValue(wd.ToUpper());
					retDt.Rows.Add(row);
				}
			}
			//shoudl be better way.. but want to finish this.. brain not working
			for (int i = 0; i < dt.Rows.Count; i++)
			{

				if (retDt.Rows.Count <= maxObjectInArray)
				{
					DataRow row = retDt.NewRow();
					var wd = dt.Rows[i]["Word"].ToString();
					row["Word"] = wd;
					row["Freq"] = dt.Rows[i]["Freq"].ToString();
					row["S_Value"] = ScrabbleValue(wd.ToUpper());
					retDt.Rows.Add(row);
				}

			}

			return retDt;

		}


		private void BuildMergedArray(string[] wordList, string challlenge)
		{
			Dictionary<string, int> freqList = LoadFrequentWords();

			List<string> three = ConstructArray(wordList, 3, 13, freqList);
			List<string> four = ConstructArray(wordList, 4, 8, freqList);
			List<string> five = ConstructArray(wordList, 5, 6, freqList);
			List<string> six = ConstructArray(wordList, 6, 4, freqList);
			string cc = string.Format("{0} was the challenge", challlenge);
			var seven = new List<string> { cc };

			IEnumerable<string> newArr = three.Union(four).Union(five).Union(six).Union(seven);
			//var newArr = seven.Union(six).Union(five).Union(four).Union(three);

			listBox2.DataSource = newArr.ToArray(); //.OrderByDescending(r => r.Length).ToArray();
		}

		private Dictionary<string, int> LoadFrequentWords()
		{
			var freqList = new Dictionary<string, int>();

			using (var reader = new StreamReader(@"freqfiltered.txt"))
			{
				while (reader.Peek() > 0)
				{
					string word = reader.ReadLine();

					if (!string.IsNullOrEmpty(word))
					{
						//freqList.Add(word);
						string[] splitted = word.Split(',');

						if (!freqList.ContainsKey(splitted[0]))
						{
							freqList.Add(splitted[0], Int32.Parse(splitted[1]));
						}

						//while (splitted[0].Length > 2 && splitted[0].Length <= 7)
						//{
						//        freqList.Add(splitted[0]);
						//}
					}
				}
			}
			return freqList;
		}

		private List<string> ConstructArray(string[] master, int wordLength, int maxObjectInArray,
											Dictionary<string, int> frequentWords)
		{
			var letterArray = new List<string>();
			List<string> items = master.Where(r => r.Length == wordLength).ToList();

			var dict = new Dictionary<string, string>();
			foreach (string word in items)
			{
				if (dict.Count <= maxObjectInArray)
				{
					int freq;
					if (frequentWords.TryGetValue(word.ToUpper(), out freq))
					{
						string mod = string.Format("word '{0}' found in freqlist with freq '{1}' ", word, freq);
						dict.Add(word, mod);
					}
				}
			}

			var wordWithValue = new Dictionary<string, int>();

			foreach (string word in items)
			{
				int val = ScrabbleValue(word.ToUpper());
				string mod = string.Format("word '{0}' not found so ordering by scrabble value '{1}'", word, val);
				wordWithValue.Add(mod, val);
			}

			if (dict.Count <= maxObjectInArray)
			{
				foreach (string word in items)
				{
					if (dict.Count <= maxObjectInArray)
					{
						if (!dict.ContainsKey(word))
						{
							int val = ScrabbleValue(word.ToUpper());
							string mod = string.Format("word '{0}' not found so ordering by scrabble value '{1}'", word, val);
							dict.Add(word, mod);
						}
					}
				}
			}

			IOrderedEnumerable<KeyValuePair<string, string>> order = dict.OrderBy(r => r.Key.Length);

			foreach (var pair in order)
			{
				letterArray.Add(pair.Value);
			}

			return letterArray;
		}

		private int ScrabbleValue(string word)
		{
			int value = 0;

			for (int i = 0; i < word.Length; i++)
			{
				char ch = word[i];
				if (ch == 'E' || ch == 'A' || ch == 'I' || ch == 'O' || ch == 'N' || ch == 'R' || ch == 'T' || ch == 'L' ||
					ch == 'S' || ch == 'U')
				{
					value += 1;
				}
				if (ch == 'D' || ch == 'G')
				{
					value += 2;
				}
				if (ch == 'B' || ch == 'C' || ch == 'M' || ch == 'P')
				{
					value += 3;
				}
				if (ch == 'F' || ch == 'H' || ch == 'V' || ch == 'W' || ch == 'Y')
				{
					value += 4;
				}
				if (ch == 'K')
				{
					value += 5;
				}
				if (ch == 'J' || ch == 'X')
				{
					value += 8;
				}
				if (ch == 'Q' || ch == 'Z')
				{
					value += 10;
				}
			}

			return value;
		}
	}
}