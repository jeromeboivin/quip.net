using Gnu.Getopt;
using libquip;
using libquip.folders;
using libquip.messages;
using libquip.threads;
using libquip.users;
using System;
using System.IO;

namespace quip
{
	class Program
	{
		static QuipUsersResponse me = null;

		enum QuipAction
		{
			none,
			newDoc,
			listRecent,
			listMyRecent
		}

		static void Main(string[] args)
		{
			if (string.IsNullOrEmpty(quip.Default.Token))
			{
				Console.Error.WriteLine("Empty API token.");
				Console.Error.WriteLine("Please go to https://quip.com/dev/token to generate your access token, and add it to the config file.");
				return;
			}

			string title = null;
			string content = null;
			string directory = null;
			QuipAction action = QuipAction.none;
			int limit = 10;

			LongOpt[] longopts = new LongOpt[9];
			longopts[0] = new LongOpt("help", Argument.No, null, 'h');
			longopts[1] = new LongOpt("title", Argument.Required, null, 't');
			longopts[2] = new LongOpt("content", Argument.Required, null, 'c');
			longopts[3] = new LongOpt("new", Argument.No, null, 'n');
			longopts[4] = new LongOpt("recent", Argument.No, null, 'r');
			longopts[5] = new LongOpt("file", Argument.Required, null, 'f');
			longopts[6] = new LongOpt("directory", Argument.Required, null, 'd');
			longopts[7] = new LongOpt("my", Argument.No, null, 'm');
			longopts[8] = new LongOpt("limit", Argument.Required, null, 'l');

			Getopt g = new Getopt("quip", args, "c:d:f:l:mn?hrt:", longopts);

			int c;
			while ((c = g.getopt()) != -1)
			{
				switch (c)
				{
					case 'c':
						content = g.Optarg;
						break;

					case 'd':
						directory = g.Optarg;
						break;

					case 'f':
						content = (File.Exists(g.Optarg)) ? File.ReadAllText(g.Optarg) : string.Empty;
						break;

					case 'l':
						if (!string.IsNullOrEmpty(g.Optarg))
						{
							Int32.TryParse(g.Optarg, out limit);
						}
						break;

					case 'm':
						action = QuipAction.listMyRecent;
						break;

					case 'n':
						action = QuipAction.newDoc;
						break;

					case 'r':
						action = QuipAction.listRecent;
						break;

					case 't':
						title = g.Optarg;
						break;

					case 'h':
					case '?':
					default:
						Usage();
						return;
				}
			}

			if (!string.IsNullOrEmpty(quip.Default.MyEmail))
			{
				QuipUser user = new QuipUser(quip.Default.Token);
				me = user.GetUser(quip.Default.MyEmail);
			}

			try
			{
				QuipThread quipThread = new QuipThread(quip.Default.Token);
				QuipMessage quipMessage = new QuipMessage(quip.Default.Token);

				switch (action)
				{
					case QuipAction.newDoc:
						var document = quipThread.NewDocument(title, content, (directory != null) ? new string[] { directory } : null, DocumentType.document, DocumentFormat.markdown);
						Console.WriteLine(document.thread.link);
						break;

					case QuipAction.listMyRecent:
					case QuipAction.listRecent:
						var recentDocs = (me == null) ? quipThread.GetRecent(limit) : quipThread.GetRecentByMembers(new string[] { me.id }, limit);
						foreach (var doc in recentDocs)
						{
							bool threadPrinted = false;
							var thread = doc.Value.thread;
							var threadMessages = quipMessage.GetMessagesForThread(thread.id);

							if (doc.Value.shared_folder_ids != null)
							{
								foreach (var shared_folder_id in doc.Value.shared_folder_ids)
								{
									QuipFolder folder = new QuipFolder(quip.Default.Token);
									var getFolderResponse = folder.GetFolder(shared_folder_id);
									PrintFolder(getFolderResponse.folder);
								}
							}

							// Get thread messages
							foreach (var message in threadMessages)
							{
								if (me != null && action == QuipAction.listMyRecent && message.author_id != me.id)
								{
									continue;
								}

								PrintThreadMessage(message);
								PrintThread(thread);
								threadPrinted = true;
							}

							if (me != null && action == QuipAction.listMyRecent && doc.Value.thread.author_id != me.id)
							{
								continue;
							}

							if (!threadPrinted)
							{
								PrintThread(thread);
							}
						}
						break;

					default:
						Usage();
						break;
				}
			}
			catch (QuipException ex)
			{
				Console.Error.WriteLine("An error occurred ({0}, code: {1}): {2}", ex.QuipError.error, ex.QuipError.error_code.ToString(), ex.QuipError.error_description);
			}
		}

		private static void PrintFolder(Folder folder)
		{
			Console.WriteLine("###############################################################################");
			Console.WriteLine($"Shared folder: {folder.title}");
			Console.WriteLine($"Shared folder id: {folder.id}");
			Console.WriteLine("###############################################################################");
			Console.WriteLine();
		}

		private static void PrintThreadMessage(Message message)
		{
			Console.WriteLine("*******************************************************************************");
			Console.WriteLine($"Message from: {message.author_name}");
			Console.WriteLine("Created: {0}", UnixTimeStampToDateTime(message.created_usec / 1000000));
			Console.WriteLine("Id: {0}", message.id);
			Console.WriteLine(message.text);
			Console.WriteLine("*******************************************************************************");
			Console.WriteLine();
		}

		private static void PrintThread(Thread thread)
		{
			Console.WriteLine(thread.title);
			Console.WriteLine("Created: {0}", UnixTimeStampToDateTime(thread.created_usec / 1000000));
			Console.WriteLine("Modified: {0}", UnixTimeStampToDateTime(thread.updated_usec / 1000000));
			Console.WriteLine("Id: {0}", thread.id);
			Console.WriteLine("Link: {0}", thread.link);
			Console.WriteLine("-------------------------------------------------------------------------------");
			Console.WriteLine();
		}

		private static void Usage()
		{
			Console.Error.WriteLine("Usage:");
			Console.Error.WriteLine("quip.exe -r/--recent [-l/--limit <limit>]");
			Console.Error.WriteLine("\tReturns the most recent threads to have received messages, similar to the inbox view in the Quip app. Default limit: 10.");
			Console.Error.WriteLine("quip.exe -m/--my [-l/--limit <limit>]");
			Console.Error.WriteLine("\tReturns the most recent threads for which I am the author. Default limit: 10.");
			Console.Error.WriteLine();
			Console.Error.WriteLine("quip.exe -n/--new -t/--title <title> -c/--content <content> [-d/--directory <destination dir>]");
			Console.Error.WriteLine("quip.exe -n/--new -t/--title <title> -f/--file <input.md> [-d/--directory <destination dir>]");
			Console.Error.WriteLine("\tCreates a document or spreadsheet.");
		}

		private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
		{
			DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
			return dateTimeOffset.DateTime;
		}
	}
}
