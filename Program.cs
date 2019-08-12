using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TwitchBot
{
    class Program
    {
        public static bool restart = false;

        static void Main(string[] args)
        {

            restrt:

            Console.WriteLine("Welcome to the Pok" + '\u00E9' + "bot console application!");
            Console.WriteLine("**Make sure to connect your bot via the LoginInfo.txt file**");
            Console.WriteLine("The bot works in chat and on the console (!pokedex not required for console)\n");

            //start a new thread to find any keyboard input
            Thread t = new Thread(readKeyboard);

            t.Start();

            Thread.Sleep(1);

            //Scan "LoginInfo.txt "file for user login info
            string channelMain = "",passMain = "",channel ="";
            string targetFile = Directory.GetCurrentDirectory() + @"\LoginInfo.txt";
            StreamReader file = new StreamReader(targetFile, true);

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    channelMain = file.ReadLine();
                }
                if (i == 1)
                {
                    passMain = file.ReadLine();
                }
                if (i == 2)
                {
                    channel = file.ReadLine();
                }
            }

            file.Close();

        refresh: //jump to here if null message is received from stream

            DateTime lastMessage = DateTime.Now; //Used to delay messages to counter bans
            GC.Collect();
            //Connect to twitch irc server with login info
            IrcClient irc = new IrcClient("irc.twitch.tv", 6667,channelMain,passMain);
            irc.joinRoom(channel);
            //Infinite loop to check any keyboard or stream input
            while (true)
            {
                bool enter = false;
                bool advance = false; //Used to know if a picture was updated
                string message = irc.readMessage();

                //Restart program if boolean value changes mid-loop
                if (restart == true)
                {
                    restart = false;
                    t.Join();
                    Console.Clear();
                    goto restrt;
                }


                //Now check for any irc messages and respond appropriately
                if (message == null)
                {
                    //Caught this stupid guy giving me problems!
                    //Console.Clear();
                    //Console.WriteLine("We hit a null message");
                    GC.Collect();
                    goto refresh;
                }
                if (message.Contains("!pokedex add"))
                {
                    irc.sendChatMessage("Adding name to database...");
                    lastMessage = DateTime.Now;
                    addName(message, lastMessage);//add name to data base
                    enter = true;
                }
                if ((message.Contains("!pokedex") || message.Contains("!pdx")) && enter == false)
                {
                    lastMessage = DateTime.Now;
                    advance = updatePic(message, lastMessage);//update pokemon.png if pokemon is found
                    if (advance == false)
                    {
                       advance = checkNames(message); //Look for a nickname and update
                    }
                    if (advance == true)
                    {
                        irc.sendChatMessage("Pok" + '\u00E9' + "dex updated"); //Let chat know we found the nickname

                    }
                    
                }

                GC.Collect();
            }
        }

        static private void readKeyboard()
        {
            while (true)
            {
                //this method reads any keyboard input on the console
                string input;
                input = Console.ReadLine();

                if (input == null || input == "")
                {
                    //if null exception, don't do anything.
                    continue;
                }
                else
                {
                    input = input.ToLower();//Accounting for case errors
                    DateTime lastMessage = DateTime.Now;

                    bool advance = false;

                    //This is great because now we can use if statements to specify any command in console!
                    //add command displayed below
                    if (input.IndexOf("add") == 0)
                    {
                        input = "!pokedex " + input;
                        addName(input, lastMessage);
                        continue;
                    }
                    if (input.IndexOf("commands") == 0 && input[input.Length - 1] == 's')
                    {
                        Console.Clear();
                        Console.WriteLine("add: Add pokemon nicknames to database. \nclear: Clears console of all text.");
                        Console.WriteLine("end: Exits program. \ncommands: Displays all console commands.");
                        Console.WriteLine("Restart: Restarts the program.\n");
                        continue;
                    }
                    if (input.IndexOf("clear") == 0)
                    {
                        Console.Clear();
                        continue;
                    }
                    if (input.IndexOf("options") == 0)
                    {
                        string option = "";
                        Console.Clear();
                        Console.WriteLine("a.)Change login info");
                        Console.Write("b.)Exit\n");

                        string targetFile = Directory.GetCurrentDirectory() + @"\LoginInfo.txt";

                        option = Console.ReadLine();
                        if (option == "a")
                        {
                            
                            try
                            {
                                StreamWriter file = new StreamWriter(targetFile, false);
                                for (int i = 0; i < 3; i++)
                                {
                                    if (i == 0)
                                        Console.WriteLine("Bot Name/Account");
                                    if (i == 1)
                                        Console.WriteLine("Oauth token");
                                    if (i == 2)
                                        Console.WriteLine("Channel room to send the bot");
                                    string option2 = "";
                                    option2 = Console.ReadLine();
                                    file.WriteLine(option2);
                                }

                                file.Close();
                                Console.WriteLine("Saved. Would you like to restart?");
                                string option3 = "";
                                option3 = Console.ReadLine();
                                if (option3.ToLower() == "yes")
                                {
                                    restart = true;
                                    Console.WriteLine("Restarting...");
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            catch (IOException)
                            {
                                Console.Clear();
                                Console.WriteLine("Could not successfully change settings");
                                Console.ReadLine();
                                Directory.Delete(targetFile, true);
                            }
                        }

                        if (option == "b")
                        {
                            continue;
                        }
                    }
                    if (input.IndexOf("end") == 0)
                    {
                        System.Environment.Exit(1);
                    }
                    if (input.IndexOf("restart") == 0)
                    {
                        restart = true;
                        Console.WriteLine("Restarting...");
                        break;
                    }

                    //if a command isn't found then we're going to assume that it's a pokemon
                    //Now user simply just has to type the pokemon they want without "!pokedex"
                    input = "!pokedex " + input;
                    advance = updatePic(input, lastMessage);

                    if (advance == false)
                    {
                        advance = checkNames(input);
                    }
                    if(advance == true)
                    {
                        Console.WriteLine("Pok" + '\u00E9' + "dex updated");
                    }


                }

            }

            Thread.Sleep(1);
            return;
        }

        static private bool checkNames(string message)
        {
            int pos = message.LastIndexOf("!pokedex ") + 9;
            if (pos == 8)
            {
                //Console.Clear();
                Console.WriteLine("Pokemon under Nickname " + message +  " doesn't exist");
                return false; //False
            }

            string newStr = "";
            for (int i = pos; i < message.Length; i++)
            {
                newStr += message[i]; 
            }

            newStr = newStr.ToUpper();
            //Console.WriteLine(newStr + "\n");
            string targetFile = Directory.GetCurrentDirectory() + @"\Nicknames.txt";

            if (!System.IO.File.Exists(targetFile))
            {
                //Console.Clear();
                Console.WriteLine("Nicknames.txt not found in current directory");
            }

            string line,word,word2;
            string[] space = new string[] {" "};
            string[] result;
            StreamReader file = new StreamReader(targetFile, true);
            while (( line = file.ReadLine()) != null)
            {
                result = line.Split(space, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < result.Length - 1; i++)
                {
                    word = result[i];
                    word2 = result[i + 1];
                    bool local = false;
                    DateTime lastMessage;

                    if (word.ToUpper() == newStr)
                    {
                        lastMessage = DateTime.Now;
                        local = updatePic("!pokedex " + word2,lastMessage);
                    }
                    if (word2.ToUpper() == newStr)
                    {
                        lastMessage = DateTime.Now;
                        local = updatePic("!pokedex " + word,lastMessage);
                    }
                    if (local == true)
                    {
                        file.Close();
                        return true;//True we found it
                    }
                }
            }
            file.Close();
            //Console.Clear();
            Console.WriteLine("Pok" + '\u00E9' + "mon " + newStr + " doesn't exist");
            return false; //False we didn't find it
        }

        static private void addName(string message, DateTime lastMessage)
        {
            int pos = message.LastIndexOf("!pokedex add ") + 13;
            if (pos == 12)
            {
                //Console.Clear();
                Console.WriteLine("Empty, no name");
                while((DateTime.Now - lastMessage > TimeSpan.FromSeconds(2)))
                {

                }

                return;
            }

            string newStr = "";
            for (int i = pos; i < message.Length; i++)
            {
                newStr += message[i];
            }

            string word = "", word2 = "";
            string[] space = new string[] { " " };
            string[] result;

            result = newStr.Split(space, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < result.Length; i++)
            {
                word = result[0];
                word2 = result[1];
            }

            string targetFile = Directory.GetCurrentDirectory() + @"\Nicknames.txt";

            if (!System.IO.File.Exists(targetFile))
            {
                System.IO.File.Create(targetFile);
            }

            try {
                StreamWriter file = new StreamWriter(targetFile, true);
                file.WriteLine(word.ToUpper() + " " +  word2.ToUpper());
                file.Close();
                //Console.Clear();
                Console.WriteLine("Nickname added to database");
            } catch (IOException)
            {
                //Console.Clear();
                Console.WriteLine("Exception Thrown, Try again?");
                Directory.Delete(targetFile,true);
            }

        }

        static private bool updatePic(string message, DateTime lastMessage)
        {
            int pos = message.LastIndexOf("!pokedex ") + 9;
            if (pos == 8)
            {
                //Console.Clear();
                Console.WriteLine("Pokemon: " + message + " was not found");
                return false;
            }
            string newStr = "";
            for (int i = pos; i < message.Length; i++)
            {
                newStr += message[i];
            }

            //DEBUG CONSOLE WRITES//
            //Console.WriteLine(message);
            //Console.WriteLine(newStr);

            string pokdir = newStr.ToUpper();
            //Console.WriteLine(pokdir + "\n\n");
            string sourcePath = Directory.GetCurrentDirectory() + @"\pokemonDir";
            string targetFile = Directory.GetCurrentDirectory() + @"\Pokemon.png";

            //Console.WriteLine(sourceFile);
            int n;
            bool isInt = int.TryParse(pokdir, out n);

            string[] dc = Directory.GetFiles(sourcePath,$"{pokdir}???.png");

            int pokP = 0;
            string pokS = "";

            if (isInt == true)
            {
                pokdir = n.ToString("D3");
                string[] dc2 = Directory.GetFiles(sourcePath, $"*{pokdir}.png");
                //Console.WriteLine(pokdir);
                for (int i = 0; i < dc2.Length; ++i)
                {
                    pokdir = dc2[i];
                }
            }

            for (int i = 0; i < dc.Length; ++i)
            {
                pokdir = @dc[i];
                
            }


            string sourceFile = System.IO.Path.Combine(sourcePath, pokdir);


            if (!System.IO.File.Exists(sourceFile))
            { 
                return false;
            }

            if (!System.IO.File.Exists(targetFile))
            {
                System.IO.File.Create(targetFile);
            }

            System.IO.File.Copy(sourceFile, targetFile, true);
            //Console.Clear();
            //Console.WriteLine("Pok" + '\u00E9' + "dex updated");

            

            //For .wav files 
            pokP = pokdir.IndexOf(".png") - 3;
            for (int i = pokP; i < pokP + 3; i++)
            {
                pokS += pokdir[i];
            }

            //Console.WriteLine(pokS); //Used for debug purposes

            while ((DateTime.Now - lastMessage > TimeSpan.FromSeconds(2)))
            {

            }

            string soundLoc = Directory.GetCurrentDirectory() + @"\pokCry\" + pokS + ".wav";
            if (System.IO.File.Exists(soundLoc))
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer();
                player.SoundLocation = soundLoc;
                player.Load();
                player.Play();
                player.Dispose();
            }

            GC.Collect();
            return true;
        }

    }
}
