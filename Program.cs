using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace TexasHoldEmPoker
{
    class Program
    {
        public static User currentUser;
        public static string user = "Guest";
        public static int balance = 1000;
        public static int BotDifficulty = 3;
        public static int CallValue = 0;
        public static List<Card> CommunityHand = new List<Card>();
        public static int Pot = 0;

        static void Main(string[] args) // Initiates a game when run
        {
            DatabaseHandler.ConnectToDatabase();
            int option;
            do
            {
                option = MainMenuOption();
                if (option == 1)
                {
                    Player player1 = new Player(user, balance);
                    Bot bot1 = new Bot("Robert", 1000, BotDifficulty);
                    Bot bot2 = new Bot("Jonathan", 1000, BotDifficulty);
                    Bot bot3 = new Bot("William", 1000, BotDifficulty);
                    List<Player> playerlist = new List<Player> { bot1, bot2, player1, bot3 };
                    while(player1.GetBalance() >= 0 && option == 1)
                    {
                        ClearGame();
                        playerlist = OpenGame(ref playerlist);
                        Stack<Card> gameDeck = CreateDeck();
                        player1.SetHand(Player.CreateHand(ref gameDeck));
                        bot1.SetHand(Player.CreateHand(ref gameDeck));
                        bot2.SetHand(Player.CreateHand(ref gameDeck));
                        bot3.SetHand(Player.CreateHand(ref gameDeck));
                        Console.WriteLine("Order of Play: {0}, {1}, {2}, {3}", playerlist[0].GetName(), playerlist[1].GetName(), playerlist[2].GetName(), playerlist[3].GetName());
                        Player[] gameArray = new Player[4];
                        playerlist.CopyTo(0, gameArray, 0, gameArray.Length);
                        List<Player> gameList = gameArray.ToList();
                        PreFlop(player1, ref gameList, 1);
                        Flop(player1, ref gameDeck, ref gameList);
                        River(player1, ref gameDeck, ref gameList);
                        Turn(player1, ref gameDeck, ref gameList);
                        DetermineWinner(gameList);
                        if (user != "Guest")
                        {
                            balance = player1.GetBalance();
                            DatabaseHandler.ChangeBalance(currentUser, player1.GetBalance());
                        }
                        else
                        {
                            balance = player1.GetBalance();
                        }
                        EvaluateGame(playerlist);
                        Console.WriteLine("");
                        Console.Write("Play another round? (1 = Yes, otherwise no) ");
                        option = Convert.ToInt32(Console.ReadLine());
                    }
                }
                else if (option == 2)
                {
                    option = Tutorial();
                }
                else if (option == 3)
                {
                    int settingsOption = SettingsOption();
                    if (settingsOption == 1)
                    {
                        SetDifficulty();
                    }
                    else if (settingsOption == 2)
                    {
                        LogIn();
                    }
                    else if(settingsOption == 3)
                    {
                        SignUp();
                    }
                }

            } while (option != 4);
        }

        private static int Tutorial() 
        {
            int option = -1;
            Console.Clear();
            Console.WriteLine("Tutorial");
            Console.WriteLine("");
            Console.WriteLine("The round starts with a player being assigned as a dealer, and a small and big blind being played by players to the left of the dealer.");
            Console.WriteLine("So if we had a list of players going player 1, player 2, player 3, player 4 where player 1 is assigned as the dealer.");
            Console.WriteLine("Then player 4 would play a small blind of fixed amount $1, and player 3 would play a big blind of fixed amount $2.");
            Thread.Sleep(20000);
            Console.Clear();
            Console.WriteLine("Each player is dealt two cards into their hand, and can decide what decision they want to make based on how good they are.");
            Console.WriteLine("Player 2 would be first to either 'call' (Match the current highest bet i.e. $2), 'raise' (Play a higher bet, following which all subsequent players will have to 'call') or 'fold' (Bet nothing and put yourself out of the round).");
            Console.WriteLine("Since player 3 put in the big blind, he has technically already called and so he can choose to 'check' (bet nothing but remain in the game) unless somebody before him has raised.");
            Thread.Sleep(20000);
            Console.Clear();
            Console.WriteLine("At the start of the 'flop' three cards are dealt onto the table. These 'community cards' can be used by any player to create a combination of cards that could win the game.");
            Console.WriteLine("Another round of betting starts. This time, players can start by checking and wait for somebody to raise. The game can still progress even if all players choose to check.");
            Thread.Sleep(15000);
            Console.Clear();
            Console.WriteLine("Next comes the 'River', where only one card is added to the community cards. Another round of betting starts.");
            Thread.Sleep(7500);
            Console.Clear();
            Console.WriteLine("Finally the 'Turn', another card is added and betting begins again.");
            Thread.Sleep(5000);
            Console.Clear();
            Console.WriteLine("Once betting for the Turn ends, the two cards in each players hand are revealed. The person with the best combination wins, if more than one player has the best combination then the player with the highest value card in the combo wins.");
            Thread.Sleep(10000);
            Console.Clear();
            Console.WriteLine("The hand rankings are as follows (worst to best):");
            Console.WriteLine("1. High Card - The highest card in your hand.");
            Console.WriteLine("2. Pair - Two cards of the same value e.g. pair of 3s.");
            Console.WriteLine("3. Two Pair - Two different pairs.");
            Console.WriteLine("4. Three of a Kind - Three cards of the same value.");
            Console.WriteLine("5. Straight - Five consecutive cards of any suit e.g. 2, 3, 4, 5, 6.");
            Console.WriteLine("6. Flush - Five cards of the same suit and any value.");
            Console.WriteLine("7. Full House - A pair and a three of a kind.");
            Console.WriteLine("8. Four of a Kind - Four cards of the same value.");
            Console.WriteLine("9. Straight Flush - Five consecutive cards of the same suit.");
            Console.WriteLine("10. Royal Flush - 10, Jack, Queen, King and Ace all from the same suit.");
            Console.WriteLine("");
            Console.WriteLine("1 - Go Back");
            Console.WriteLine("");
            while (option != 1)
            {
                try
                {
                    Console.Write("Your Option: ");
                    option = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Please enter a valid value");
                }
            }
            return option;
        }

        private static void LogIn() 
        {
            Console.Clear();
            string username = "";
            string password = "";
            do
            {
                Console.Write("Username: ");
                username = Console.ReadLine();
                Console.Write("Password: ");
                password = Console.ReadLine();
            } while (!DatabaseHandler.AttemptLogin(username, password));

            currentUser = DatabaseHandler.Login(username, password);
            user = currentUser.GetUsername();
            balance = currentUser.GetBalance(); 
        }

        private static void SignUp()
        {
            Console.Clear();
            string username;
            string password;
            do
            {
                Console.Write("Enter a new username (2-15 characters): ");
                username = Console.ReadLine();
            } while (!Validation.ValidateUsername(username));
            do
            {
                Console.Write("Enter a new password (Must contain 8-15 characters, at least one number and at least one uppercase letter): ");
                password = Console.ReadLine();
            } while (!Validation.ValidatePassword(password));
            User newUser = new User(username, password, 1000);
            DatabaseHandler.RegisterUser(newUser);
        }

        private static int MainMenuOption()
        {
            int option = -1;
            Console.Clear();
            Console.WriteLine("Welcome, {0}", user);
            Console.WriteLine("Your balance is {0}", balance);
            Console.WriteLine("");
            Console.WriteLine("Console Poker - Main Menu");
            Console.WriteLine("1 - Play");
            Console.WriteLine("2 - Tutorial");
            Console.WriteLine("3 - Settings");
            Console.WriteLine("4 - Exit");
            Console.WriteLine("");
            while(option < 1 || option > 4)
            {
                try
                {
                    Console.Write("Your Option: ");
                    option = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Please enter a valid value");
                }
            }
            return option;
        }

        private static int SettingsOption()
        {
            int option = -1;
            Console.Clear();
            Console.WriteLine("Settings");
            Console.WriteLine("1 - Bot Difficulty");
            Console.WriteLine("2 - Log In");
            Console.WriteLine("3 - Sign Up");
            Console.WriteLine("4 - Go Back");
            Console.WriteLine("");
            while (option < 1 || option > 4)
            {
                try
                {
                    Console.Write("Your Option: ");
                    option = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Please enter a valid value");
                }
            }
            return option;
        }

        private static void SetDifficulty()
        {
            BotDifficulty = 0;
            Console.Clear();
            Console.WriteLine("Bot Difficulty");
            Console.WriteLine("3 - Easy (Bots will rarely raise)");
            Console.WriteLine("2 - Medium (Bots will raise if they have a good hand)");
            Console.WriteLine("1 - Hard (Bots will raise often");

            while (BotDifficulty < 1 || BotDifficulty > 3)
            {
                try
                {
                    Console.Write("Option: ");
                    BotDifficulty = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: Please enter a valid value");
                }
            }
        }

        private static void ClearGame() 
        {
            Console.Clear();
            Pot = 0;
            CallValue = 0;
            CommunityHand.Clear();
        }

        private static Queue<Player> CreatePlayerQueue(List<Player> players) // Creates a queue of players
        {
            Queue<Player> playerQueue = new Queue<Player>();
            foreach (Player p in players)
            {
                playerQueue.Enqueue(p);
            }

            return playerQueue;
        }

        private static void DisplayPot()
        {
            Console.WriteLine("The Pot is currently at {0}", Pot);
        }

        private static void PreFlop(Player player, ref List<Player> players, int blindOffset) // After blinds and players have received cards
        {
            Console.WriteLine(""); // Just makes the game look cleaner
            int numCalls = 0; 
            Queue<Player> playerQueue = CreatePlayerQueue(players); // Creates a queue of players based on the list passed in by reference.
            DisplayPot(); 
            DisplayBalance(player);
            DisplayCards(player.GetHand());
            Console.WriteLine("");

            while (numCalls != players.Count) // While not every player has called 
            {
                int bet = playerQueue.Peek().MakeBet(CallValue, CommunityHand); // Takes the first person at the top of the queue and runs the MakeBet method..
                if (bet > CallValue) // If player has raised
                {
                    Console.WriteLine("{0} has chosen to raise to {1}.", playerQueue.Peek().GetName(), bet);
                    CallValue = bet;
                    Pot += bet;
                    numCalls = 1; // All players must now match this bet
                    blindOffset = 0; // Not counting for blinds anymore
                    DisplayPot();
                }
                else if (bet == CallValue) // If player has called
                {
                    if (CallValue == 0) // Check
                    {
                        Console.WriteLine("{0} has chosen to check", playerQueue.Peek().GetName());
                        numCalls++;
                    }
                    else
                    {
                        Console.WriteLine("{0} has chosen to call.", playerQueue.Peek().GetName());
                        Pot += bet;
                        numCalls++;
                    }
                    DisplayPot();
                }
                else if (bet == -1) // A bet of -1 means a fold
                {
                    Console.WriteLine("{0} has chosen to fold.", playerQueue.Peek().GetName());
                    players.Remove(playerQueue.Peek());
                }
                playerQueue.Dequeue(); // A player has taken their turn so they are removed from the queue
                if (numCalls == players.Count - blindOffset) // If all the players that can call have called then the CallValue is now reset.
                {
                    CallValue = 0;
                }

                if (playerQueue.Count() == 0 && numCalls != players.Count()) // If the queue is empty and not all players have called then it makes another queue
                {
                    playerQueue = CreatePlayerQueue(players);
                }
            }
        }

        private static void Flop(Player player, ref Stack<Card> deck, ref List<Player> players) // Runs preflop but with extra cards
        {
            Console.WriteLine("");
            if (players.Count == 1)
            {
                return;
            }
            CallValue = 0;
            for (int i = 0; i < 3; i++)
            {
                CommunityHand.Add(deck.Pop());
            }
            DisplayCommunityHand();
            PreFlop(player, ref players, 0);
        }

        private static void River(Player player, ref Stack<Card> deck, ref List<Player> players)
        {
            Console.WriteLine("");
            if (players.Count == 1)
            {
                return;
            }
            CallValue = 0;
            CommunityHand.Add(deck.Pop());
            DisplayCommunityHand();
            PreFlop(player, ref players, 0);
        }

        private static void Turn(Player player, ref Stack<Card> deck, ref List<Player> players)
        {
            Console.WriteLine("");
            if (players.Count == 1)
            {
                return;
            }
            CallValue = 0;
            CommunityHand.Add(deck.Pop());
            DisplayCommunityHand();
            PreFlop(player, ref players, 0);
        }

        private static void DetermineWinner(List<Player> players)
        {
            // Creates a new list of players based on their hand
            List<Player> endPlayers = players.OrderByDescending(p => HandCalculator.HandRanking(p.GetHand().Concat(CommunityHand).ToArray())).ToList(); 
            if (endPlayers.Count == 1) // Last person left
            {
                endPlayers[0].SetBalance(endPlayers[0].GetBalance() + Pot);
                Console.WriteLine("{0} wins the pot!", endPlayers[0].GetName());
            }
            // else if there is tiebreaker
            else if (HandCalculator.HandRanking(endPlayers[0].GetHand().Concat(CommunityHand).ToArray()) == HandCalculator.HandRanking(endPlayers[1].GetHand().Concat(CommunityHand).ToArray())) 
            {
                List<Player> tieBreakers = new List<Player>(); 
                int highestRank = HandCalculator.HandRanking(endPlayers[0].GetHand().Concat(CommunityHand).ToArray()); // Takes the top rank
                foreach (Player p in endPlayers)
                {
                    if (HandCalculator.HandRanking(p.GetHand().Concat(CommunityHand).ToArray()) == highestRank)
                    {
                        tieBreakers.Add(p); // Adds the players who are tied to the list
                    }
                }
                TieBreaker(ref tieBreakers, highestRank); // Sorts the players by their highest card in the hand
                tieBreakers[0].SetBalance(tieBreakers[0].GetBalance() + Pot);
                Console.WriteLine("{0} wins the pot!", tieBreakers[0].GetName());
            }
            else
            {
                endPlayers[0].SetBalance(endPlayers[0].GetBalance() + Pot); // Takes the person with the highest rank
                Console.WriteLine("{0} wins the pot!", endPlayers[0].GetName());
            }
        }

        private static void TieBreaker(ref List<Player> players, int rank) // Only goes up to Three of a Kind as two straights is highly unlikely
        {
            int highestValue = 0;
            if (rank == 4)
            {
                for(int i = 0; i < players.Count; i++)
                {
                    Card[] hand = players[i].GetHand().Concat(CommunityHand).ToArray();
                    for (int j = 0; j < hand.Length; j++)
                    {
                        if (hand.Count(c => c.GetValue() == hand[i].GetValue()) == 3)
                        {
                            if (hand[j].GetValue() > highestValue)
                            {
                                highestValue = hand[j].GetValue();
                                Player highestScoringPlayer = players[i];
                                players.Remove(highestScoringPlayer);
                                players.Insert(0, highestScoringPlayer);
                            }
                        }
                    }
                }
            }
            else if (rank == 3)
            {
                for(int i = 0; i < players.Count; i++)
                {
                    Card[] hand = players[i].GetHand().Concat(CommunityHand).ToArray();
                    int checkValue = 0;
                    for (int j = 0; j < hand.Length; j++)
                    {
                        checkValue = hand[i].GetValue();
                        for (int k = j + 1; k < hand.Length; k++)
                        {
                            if (hand[k].GetValue() == checkValue)
                            {
                                if (hand[j].GetValue() > highestValue)
                                {
                                    highestValue = hand[j].GetValue();
                                    Player highestScoringPlayer = players[i];
                                    players.Remove(highestScoringPlayer);
                                    players.Insert(0, highestScoringPlayer);
                                }
                            }
                        }
                    }
                }
            }
            else if (rank == 2)
            {
                for(int i = 0; i < players.Count; i++)
                {
                    Card[] hand = players[i].GetHand().Concat(CommunityHand).ToArray();
                    for (int j = 0; j < hand.Length; j++)
                    {
                        if (hand.Count(c => c.GetValue() == hand[j].GetValue()) == 2)
                        {
                            if (hand[j].GetValue() > highestValue)
                            {
                                highestValue = hand[j].GetValue();
                                Player highestScoringPlayer = players[i];
                                players.Remove(highestScoringPlayer);
                                players.Insert(0, highestScoringPlayer);
                            }
                        }
                    }
                }
            }
            else if (rank == 1)
            {
                for(int i = 0; i < players.Count; i++)
                {
                    Card[] hand = players[i].GetHand().Concat(CommunityHand).ToArray();
                    for (int j = 0; j < hand.Length; j++)
                    {
                        if (hand[j].GetValue() > highestValue)
                        {
                            highestValue = hand[j].GetValue();
                            Player highestScoringPlayer = players[i];
                            players.Remove(highestScoringPlayer);
                            players.Insert(0, highestScoringPlayer);
                        }
                    }
                }
            }
        }

        private static void EvaluateGame(List<Player> players) // Shows the cards of all players and what hand they had
        {
            string rank = "";
            foreach (Player p in players)
            {
                if (RankToString().TryGetValue(HandCalculator.HandRanking(p.GetHand().Concat(CommunityHand).ToArray()), out string stringRank))
                {
                    rank = stringRank;
                }
                Console.WriteLine("{0}'s cards were {1} of {2} and {3} of {4}, they had a {5}", p.GetName(), ValueToSuit(p.GetHand()[0].GetValue()),
                    p.GetHand()[0].GetSuit(), ValueToSuit(p.GetHand()[1].GetValue()), p.GetHand()[1].GetSuit(), rank);
            }
        }

        private static void DisplayCards(Card[] hand)
        {
            Console.WriteLine("Your cards are: {0} of {1}, {2} of {3}", ValueToSuit(hand[0].GetValue()), hand[0].GetSuit(), ValueToSuit(hand[1].GetValue()), hand[1].GetSuit());
        }

        private static void DisplayBalance(Player player)
        {
            Console.WriteLine("Your balance is: {0}", player.GetBalance());
        }

        private static string ValueToSuit(int value)
        {
            switch (value)
            {
                case 14:
                    return "Ace";
                case 13:
                    return "King";
                case 12:
                    return "Queen";
                case 11:
                    return "Jack";
                default:
                    return value.ToString();
            }
        }

        private static void DisplayCommunityHand()
        {
            foreach (Card c in CommunityHand)
            {
                Console.WriteLine(ValueToSuit(c.GetValue()) + " of " + c.GetSuit());
            }
        }

        private static List<Player> OpenGame(ref List<Player> players) // Opens the round with blinds, then reorders the list so that it is in order of play
        {
            players[1].SetBalance(players[1].GetBalance() - 1); // Small blind
            Console.WriteLine("Small Blind $1: {0}", players[1].GetName());
            players[2].SetBalance(players[2].GetBalance() - 2); // Big blind
            Console.WriteLine("Big Blind $2: {0}", players[2].GetName());

            Pot = 3;
            CallValue = 2;
            Player firstToPlay = players.Last();
            players.Remove(firstToPlay);
            players.Insert(0, firstToPlay);
            return players;
        }

        private static Stack<Card> CreateDeck() // Creates a deck
        {
            Stack<Card> deck = Card.GenerateDeck();
            Card.Shuffle(deck);
            return deck;
        }

        private static Dictionary<int, string> RankToString()
        {
            var rankings = new Dictionary<int, string>();
            rankings.Add(10, "Royal Flush");
            rankings.Add(9, "Straight Flush");
            rankings.Add(8, "Four of a Kind");
            rankings.Add(7, "Full House");
            rankings.Add(6, "Flush");
            rankings.Add(5, "Straight");
            rankings.Add(4, "Three of a Kind");
            rankings.Add(3, "Two Pair");
            rankings.Add(2, "Pair");
            rankings.Add(1, "High Card");

            return rankings;
        }
    }

    public class Player
    {
        protected string Name;
        protected int Balance;
        protected Card[] Hand;

        public Player(string Name, int Balance)
        {
            this.Name = Name;
            this.Balance = Balance;
            this.Hand = new Card[2];
        }

        public int GetBalance()
        {
            return Balance;
        }

        public void SetBalance(int newBalance)
        {
            if (newBalance > 0)
            {
                Balance = newBalance;
            }
        }

        public static Card[] CreateHand(ref Stack<Card> deck) // Creates a hand for a player and populates it
        {
            Card[] hand = new Card[2];
            for (int i = 0; i < hand.Length; i++)
            {
                hand[i] = deck.Pop();
            }
            return hand;
        }

        public void SetHand(Card[] newHand)
        {
            Hand = newHand;
        }

        public string GetName()
        {
            return Name;
        }

        public Card[] GetHand()
        {
            return Hand;
        }

        public virtual int MakeBet(int callValue, List<Card> communityHand) // Returns the bet the user has made
        {
            int bet = 0;
            string option = BetOption(callValue);
            if (option == "raise")
            {
                while (bet <= callValue || bet <= 2 || bet > Balance)
                {
                    try
                    {
                        Console.Write("Enter amount to raise: ");
                        bet = Convert.ToInt32(Console.ReadLine());
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Error: Please enter a valid value");
                    }
                }
                SetBalance(Balance - bet);
                return bet;
            }
            else if (option == "call")
            {
                SetBalance(Balance - callValue);
                return callValue;
            }
            else if (option == "check")
            {
                return 0;
            }
            return -1;
        }

        private static string BetOption(int callValue) // Asks the user what option they'd like to choose
        {
            string option = "";
            if (callValue == 0)
            {
                do
                {
                    Console.Write("Check, raise or fold? ");
                    option = Console.ReadLine().ToLower();
                } while (!TurnDictionary().TryGetValue(option, out int turn) || option == "call");
            }
            else
            {
                do
                {
                    Console.Write("Call, raise or fold? ");
                    option = Console.ReadLine().ToLower();
                } while (!TurnDictionary().TryGetValue(option, out int turn) || option == "check");
            }

            return option;
        }

        private static Dictionary<string, int> TurnDictionary() // Dictionary for all the possible options
        {
            var turns = new Dictionary<string, int>();
            turns.Add("raise", 3);
            turns.Add("call", 2);
            turns.Add("check", 1);
            turns.Add("fold", 0);

            return turns;
        }
    }

    public class Bot : Player
    {
        private int Difficulty;

        public Bot(string Name, int Balance, int Difficulty) : base(Name, Balance)
        {
            this.Difficulty = Difficulty;
            Hand = new Card[2];
        }

        public int GetDifficulty()
        {
            return Difficulty;
        }

        public override int MakeBet(int callValue, List<Card> communityHand) // Returns the bet the bot has made based on their decision.
        {
            List<Card> comboList = Hand.ToList();

            if (communityHand.Count > 0)
            {
                foreach (Card c in communityHand)
                {
                    comboList.Add(c);
                }
            }
            Card[] comboHand = comboList.ToArray();
            int cardValueAverage = (GetHand()[0].GetValue() + GetHand()[1].GetValue()) / 2;
            int decisionValue = HandCalculator.HandRanking(comboHand) * (cardValueAverage / Difficulty);

            if (MakeDecision(callValue, communityHand) == "raise")
            {
                if(decisionValue <= callValue + cardValueAverage && decisionValue > callValue) // If the player has bluffed and their bet won't be high enough normally
                {
                    SetBalance(Balance - decisionValue); // Bet the value of their decisionValue, providing it is greater than the callvalue
                    return decisionValue;
                }
                else
                {
                    SetBalance(Balance - decisionValue / Difficulty);
                    return decisionValue / Difficulty;
                }
            }
            else if (MakeDecision(callValue, communityHand) == "call")
            {
                SetBalance(Balance - callValue);
                return callValue;
            }
            else if (MakeDecision(callValue, communityHand) == "check")
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private string MakeDecision(int callValue, List<Card> communityHand) // Returns the decision the bot has made.
        {
            System.Random rnd = new System.Random();
            List<Card> comboList = Hand.ToList();

            if (communityHand.Count != 0)
            {
                foreach (Card c in communityHand)
                {
                    comboList.Add(c);
                }
            }
            Card[] comboHand = comboList.ToArray();
            double cardValueAverage = (Hand[0].GetValue() + Hand[1].GetValue()) / 2;
            int bluff = rnd.Next(1, 50 - Difficulty*2 + 1);
            double decisionValue = HandCalculator.HandRanking(comboHand) * (cardValueAverage / Difficulty);

            if (decisionValue > callValue + cardValueAverage || bluff == (50 - Difficulty*2))
            {
                return "raise";
            }
            else if (decisionValue > callValue / Difficulty)
            {
                if (callValue == 0)
                {
                    return "check";
                }
                return "call";
            }
            else
            {
                return "fold";
            }
        }
    }

    public class Card 
    {
        private string Suit;
        private int Value;
        public static string[] suits = new string[] { "Clubs", "Diamonds", "Hearts", "Spades" };
        public static int[] values = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };

        public Card(int Value, string Suit) // Generates a card object with a value and a suit
        {
            this.Value = Value;
            this.Suit = Suit;
        }

        public static Stack<Card> GenerateDeck() // Generates an ordered deck using the arrays
        {
            Stack<Card> newDeck = new Stack<Card>();
            for (int i = 0; i < suits.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    newDeck.Push(new Card(values[j], suits[i]));
                }
            }

            return newDeck;
        }

        public static void Shuffle<T>(Stack<T> stack) // Shuffles a stack / deck of cards in this case
        {
            System.Random rnd = new System.Random();
            var values = stack.ToArray();
            stack.Clear();
            foreach (var value in values.OrderBy(x => rnd.Next()))
            {
                stack.Push(value);
            }
        }

        public int GetValue()
        {
            return Value;
        }

        public string GetSuit()
        {
            return Suit;
        }
    }

    public static class HandCalculator // Calculates the ranking of a combination hand
    {
        public static int HandRanking(Card[] hand)
        {
            string handRank = "";

            if (RoyalFlush(hand))
            {
                handRank = "RF";
            }
            else if (StraightFlush(hand))
            {
                handRank = "SF";
            }
            else if (FourOfAKind(hand))
            {
                handRank = "FOAK";
            }
            else if (FullHouse(hand))
            {
                handRank = "FH";
            }
            else if (Flush(hand))
            {
                handRank = "F";
            }
            else if (Straight(hand))
            {
                handRank = "S";
            }
            else if (ThreeOfAKind(hand))
            {
                handRank = "TOAK";
            }
            else if (TwoPair(hand))
            {
                handRank = "TP";
            }
            else if (Pair(hand))
            {
                handRank = "P";
            }
            else
            {
                handRank = "HC";
            }

            if (RankDictionary().TryGetValue(handRank, out int ranking))
            {
                return ranking;
            }
            else
            {
                return -1;
            }
        }

        private static void SortArrayValues(ref Card[] hand) // Simple bubblesort
        {
            Card temp;
            for (int i = 0; i < hand.Length; i++) // Outer loop
            {
                for (int j = i + 1; j < hand.Length; j++) // Inner loop always 1 position ahead from i
                {
                    if (hand[i].GetValue() > hand[j].GetValue()) // If it's bigger then swap
                    {
                        temp = hand[i];
                        hand[i] = hand[j];
                        hand[j] = temp;
                    }
                }
            }
        }

        private static bool Pair(Card[] hand) // Finds a pair
        {
            for (int i = 0; i < hand.Length; i++) // Loops through the hand
            {
                if (hand.Count(c => c.GetValue() == hand[i].GetValue()) == 2) // Counts the number of cards with the value of the current card in the loop
                {
                    return true; // As soon as two cards of the same value are found it returns true and doesn't need to go through the rest of the array
                }
            }

            return false;
        }

        private static bool TwoPair(Card[] hand) // Finds two pairs
        {
            int checkValue = 0;
            int pairCount = 0;
            for (int i = 0; i < hand.Length; i++)
            {
                checkValue = hand[i].GetValue(); // Gets the value of the current card in the loop (our 'base' card)
                for (int j = i + 1; j < hand.Length; j++) // Loops through the rest of the array, starting with the card next to the 'base' one.
                {
                    if (hand[j].GetValue() == checkValue) // Compares the value of the 'base' card with the current card
                    {
                        pairCount++; // Counts it as a pair
                    }
                }
            }

            if (pairCount == 2) // We only want to check for two pairs
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ThreeOfAKind(Card[] hand) // Finds a three of a kind
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand.Count(c => c.GetValue() == hand[i].GetValue()) == 3) // Same principle as the Pair() subroutine except it checks for three cards of same value
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Straight(Card[] hand)
        {
            SortArrayValues(ref hand); // Sorts the cards in ASCENDING order
            int straightCount = 1;
            int difference = 1;
            for (int i = 0; i < hand.Length; i++)
            {
                straightCount = 1; // Resets values
                difference = 1;
                for (int j = i + 1; j < hand.Length ; j++)
                {
                    if (hand[i].GetValue() == hand[j].GetValue() - difference) // Compares the current card to every other card and if it is consecutive then:
                    {
                        difference++; // Increase the difference required
                        straightCount++; // Increase the count
                        if (straightCount == 5)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool Flush(Card[] hand)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand.Count(c => c.GetSuit() == hand[i].GetSuit()) >= 5) // Counts the number of cards with the same suit as hand[i].
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FullHouse(Card[] hand)
        {
            if (Pair(hand) && ThreeOfAKind(hand)) // Checks for both a pair AND a three of a kind in the hand
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool FourOfAKind(Card[] hand)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand.Count(c => c.GetValue() == hand[i].GetValue()) == 4) // Same principle as pair but we check for 4 cards of same value
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StraightFlush(Card[] hand)
        {
            SortArrayValues(ref hand); // Sorts cards in ascending order
            int sfCount = 1;
            int difference = 1;
            for (int i = 0; i < hand.Length; i++)
            {
                sfCount = 1;
                difference = 1;
                for (int j = i + 1; j < hand.Length; j++)
                {
                    if (hand[i].GetValue() == hand[j].GetValue() - difference && hand[i].GetSuit() == hand[j].GetSuit())
                    {
                        difference++;
                        sfCount++;
                        if (sfCount == 5)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool RoyalFlush(Card[] hand)
        {
            SortArrayValues(ref hand);
            int rfCount = 1; // Start the count
            int difference = 1;
            for (int k = 0; k < hand.Length; k++) // Search through array
            {
                if (hand[k].GetValue() == 10) // For a 10
                {
                    for (int i = k; i < hand.Length; i++) // Start search process on that index
                    {
                        rfCount = 1;
                        difference = 1;
                        for (int j = i + 1; j < hand.Length; j++)
                        {
                            if (hand[i].GetValue() == hand[j].GetValue() - difference && hand[i].GetSuit() == hand[j].GetSuit())
                            {
                                difference++;
                                rfCount++;
                                if (rfCount == 5)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static Dictionary<string, int> RankDictionary()
        {
            var rankings = new Dictionary<string, int>();
            rankings.Add("RF", 10);
            rankings.Add("SF", 9);
            rankings.Add("FOAK", 8);
            rankings.Add("FH", 7);
            rankings.Add("F", 6);
            rankings.Add("S", 5);
            rankings.Add("TOAK", 4);
            rankings.Add("TP", 3);
            rankings.Add("P", 2);
            rankings.Add("HC", 1);
            rankings.Add("Error", 0);

            return rankings;
        }
    }

    static class DatabaseHandler
    {
        const string USERPATH = "";
        const string FILENAME = "PokerDatabase.sqlite";
        static string ConnectionString = "Data Source=" + USERPATH + FILENAME + ";Version=3;datetimeformat=CurrentCulture;";

        public static void ConnectToDatabase()
        {
            if (!File.Exists(USERPATH + FILENAME))
            {
                CreateDatabase();
            }
        }

        private static void CreateDatabase()
        {
            SQLiteConnection.CreateFile(USERPATH + FILENAME);
            using(SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();

                string sql = @"CREATE TABLE Users (Username varchar(12) PRIMARY KEY, Password varchar(16), Balance int);";

                using(SQLiteCommand command = new SQLiteCommand(sql, c))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void RegisterUser(User newUser)
        {
            using(SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                string sql = @"INSERT INTO Users VALUES (" + newUser.ToString() + ");";

                using(SQLiteCommand command = new SQLiteCommand(sql, c))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ChangeBalance(User currentUser, int balance)
        {
            using(SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                string sql = @"UPDATE Users SET Balance=" + balance + " WHERE Username='" + currentUser.GetUsername() +  "';";

                using(SQLiteCommand command = new SQLiteCommand(sql, c))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static bool AttemptLogin(string username, string password)
        {
            using(SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                string sql = @"SELECT * FROM Users WHERE Username='" + username + "' AND Password='" + password + "';";
                using(SQLiteCommand command = new SQLiteCommand(sql, c))
                {
                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static User Login(string username, string password)
        {
            using(SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                string sql = @"SELECT * FROM Users WHERE Username='" + username + "' AND Password='" + password + "';";
                using(SQLiteCommand command = new SQLiteCommand(sql, c))
                {
                    using(SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            User newUser = new User(reader.GetString(0), reader.GetString(1), reader.GetInt32(2));
                            return newUser;
                        }
                    }
                }
                return null;
            }
        }
    }

    class User
    {
        private string Username, Password;
        private int Balance;

        public User(string username, string password, int balance)
        {
            Username = username;
            Password = password;
            Balance = balance;
        }

        public string GetUsername()
        {
            return Username;
        }

        public int GetBalance()
        {
            return Balance;
        }
        
        public void SetBalance(int newBalance)
        {
            if(newBalance > 0)
            {
                Balance = newBalance;
            }
        }

        public override string ToString()
        {
            return String.Format("'{0}', '{1}', '{2}'", Username, Password, Balance);
        }
    }

    static class Validation
    {
        public static bool ValidateUsername(string username)
        {
            var hasMin3Chars = new Regex(@".{3,}");
            return hasMin3Chars.IsMatch(username);
        }

        public static bool ValidatePassword(string password)
        {
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMin8Chars = new Regex(@".{8,}");

            return hasNumber.IsMatch(password) && hasUpperChar.IsMatch(password) && hasMin8Chars.IsMatch(password);
        }
    }
}
