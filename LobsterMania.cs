using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lobstermania
{
    enum Symbol
    {
        Wild,
        Lobstermania,
        Buoy,
        Boat,
        Lighthouse,
        Tuna,
        Clam,
        Seagull,
        Starfish,
        Bonus,
        Scatter
    }

    class LobsterMania
    {
        const int ReelCount = 5;
        const int GameBoardRowCount = 3;
        const int PayLineCountMax = 15;

        //
        // Reels 1, 2, and 3 have all 11 symbols, while Reels 4 and 5 do not have the Bonus symbol
        //
        static readonly Symbol[] Reel123Symbols =
        {
            Symbol.Wild,
            Symbol.Lobstermania,
            Symbol.Buoy,
            Symbol.Boat,
            Symbol.Lighthouse,
            Symbol.Tuna,
            Symbol.Clam,
            Symbol.Seagull,
            Symbol.Starfish,
            Symbol.Bonus,
            Symbol.Scatter,
        };

        static readonly Symbol[] Reel45Symbols =
        {
            Symbol.Wild,
            Symbol.Lobstermania,
            Symbol.Buoy,
            Symbol.Boat,
            Symbol.Lighthouse,
            Symbol.Tuna,
            Symbol.Clam,
            Symbol.Seagull,
            Symbol.Starfish,
            Symbol.Scatter,
        };

        static readonly int[][] SymbolCounts = new int[][]
        {
            new int[] { 2, 4, 4, 6, 5, 6, 6, 5, 5, 2, 2 },
            new int[] { 2, 4, 4, 4, 4, 4, 6, 6, 5, 5, 2 },
            new int[] { 1, 3, 5, 4, 6, 5, 5, 5, 6, 6, 2 },
            new int[] { 4, 4, 4, 4, 6, 6, 6, 6, 8, 2 },
            new int[] { 2, 4, 5, 4, 7, 7, 6, 6, 7, 2 }
        };

        static readonly int[,] Payouts =
        {
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 5, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 100, 40, 25, 25, 10, 10, 5, 5, 5, 331, 5 },
            { 500, 200, 100, 100, 50, 50, 30, 30, 30, 0,25 },
            { 10000, 1000, 500, 500, 500, 250, 200, 200, 150, 0, 200 }
        };

        static readonly Random rand = new Random();

        readonly Symbol[][] reels = new Symbol[][]
        {
            new Symbol[47],
            new Symbol[46],
            new Symbol[48],
            new Symbol[50],
            new Symbol[50]
        };

        readonly Symbol[,] gameBoard = new Symbol[GameBoardRowCount, ReelCount];
        public int activePaylines = PayLineCountMax;

        // Print flags
        public bool printReels = false;
        public bool printGameboard = false;
        public bool printPaylines = false;

        public Stats stats = new Stats();

        public LobsterMania()
        {
            BuildReels();

            for(int i = 0; i < ReelCount; i++)
                rand.Shuffle(reels[i]);
        }

        void BuildReels()
        {
            for (int i = 0; i < 3; i++)
            {
                int idx = 0;
                for (int j = 0; j < Reel123Symbols.Length; j++)
                    for (int k = 0; k < SymbolCounts[i][j]; k++)
                        reels[i][idx++] = Reel123Symbols[j];
            }

            for (int i = 3; i < 5; i++)
            {
                int idx = 0;
                for (int j = 0; j < Reel45Symbols.Length; j++)
                    for (int k = 0; k < SymbolCounts[i][j]; k++)
                        reels[i][idx++] = Reel45Symbols[j];
            }
        }

        void PrintReels()
        {
            for(int num=0; num < ReelCount; num++)
            {
                Console.Write("\nReel[{0}]: [  ", num);
                for (int s = 0; s < reels[num].Length - 1; s++)
                    Console.Write("{0,12}, ", reels[num][s]);
                Console.WriteLine("{0,12} ]", reels[num][reels[num].Length-2]);
            }
        }

        public void Spin()
        {
            stats.spins++;
            if (printReels)
                PrintReels();
            UpdateGameboard();
            if (printGameboard)
                PrintGameboard();

            if (printPaylines)
            {
                Console.WriteLine("WINNING PAY LINES");
                Console.WriteLine("-----------------");
            }

            int payLineCount = 0;
            foreach(var payLine in GetPayLines().Take(activePaylines))
            {
                payLineCount++;
                int linePayout = GetLinePayout(payLine); // will include any bonus win
                if (linePayout > 0)
                {
                    stats.igWin += linePayout;
                    stats.hitCount++;
                    stats.paybackCredits += linePayout;

                    if (printPaylines)
                        PrintPayline(payLineCount, payLine, linePayout);
                }
            }

            int scatterWin = GetScatterWinCredits();
            if (scatterWin > 0)
            {
                stats.scatterWinCount++;
                stats.scatterWinCredits += scatterWin;
                stats.paybackCredits += scatterWin;
                stats.hitCount++;
                //$ REVIEW: Really?
                stats.igScatterWin = scatterWin; // only 1 scatter win allowed per game
                stats.igWin += scatterWin; // add to total game win
            }
        }

        int GetBonusCount(in Symbol [] line, out int bonusWin)
        {
            int count = 1;
            bonusWin = 0;

            for (int i = 1; i < 3; i++)
            {
                if (line[i] == Symbol.Bonus)
                    count++;
                else
                    break;
            }

            if (count == 3)
            {
                bonusWin = Bonus.GetPrizes(rand);
                stats.bonusWinCount++;
                stats.bonusWinCredits += bonusWin;
                stats.igBonusWin += bonusWin;
            }

            return count;
        }

        int GetWildCount(in Symbol [] line, ref Symbol sym)
        {
            var altSym = Symbol.Wild;
            int count = 1;

            for (int i = 1; i < ReelCount; i++)
            {
                if ((line[i] == sym) || (line[i] == altSym))
                {
                    count++;
                }
                else
                {
                    if (line[i] != Symbol.Bonus &&
                        line[i] != Symbol.Scatter &&
                        altSym == Symbol.Wild)
                    {
                        altSym = line[i];
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            sym = altSym; // count and sym are now set correctly 

            // ANOMOLY FIX
            // 3 wilds pay more than 4 of anything but lobstermania
            // 4 wilds pay more than 5 of anything but lobstermania
            // Take greatest win possible

            // Leading 4 wilds
            if ((line[1] == Symbol.Wild) &&
                (line[2] == Symbol.Wild) &&
                (line[3] == Symbol.Wild))
            {
                if (line[4] == Symbol.Lobstermania)
                {
                    sym = Symbol.Lobstermania;
                    count = 5;
                }
                else
                if (line[4] != Symbol.Wild)
                {
                    sym = Symbol.Wild;
                    count = 4;
                }
            }

            // Leading 3 wilds
            if ((line[1] == Symbol.Wild) &&
                (line[2] == Symbol.Wild) &&
                (line[3] == Symbol.Lobstermania) &&
                (line[4] == Symbol.Wild) &&
                (line[4] != Symbol.Lobstermania))
            {
                sym = Symbol.Lobstermania;
                count = 4;
                return count;
            }
            if ((line[1] == Symbol.Wild) &&
                (line[2] == Symbol.Wild) &&
                (line[3] != Symbol.Lobstermania) &&
                (line[3] != Symbol.Wild) &&
                (line[4] != line[3]))
            {
                sym = Symbol.Wild;
                count = 3;
            }

            if ((sym == Symbol.Wild) && (count == 5))
                stats.numJackpots++;

            return count;
        }

        int GetMiscCount(in Symbol [] line)
        {
            var sym = line[0];
            int count = 1;

            for (int i = 1; i < ReelCount; i++)
            {
                if ((line[i] == sym) || (line[i] == Symbol.Wild))
                    count++;
                else
                    break;
            }

            return count;
        }

        int GetLinePayout(in Symbol [] line)
        {
            int count;
            var sym = line[0];
            switch (sym)
            {
            case Symbol.Bonus:
                count = GetBonusCount(line, out int bonusWin);
                return bonusWin > 0 ? bonusWin : Payouts[count - 1, (int)sym];

            case Symbol.Scatter:
                // Scatter handled at gameboard level
                return 0;

            case Symbol.Wild:
                count = GetWildCount(line, ref sym);
                break;

            default:
                count = GetMiscCount(line);
                break;
            }

            return Payouts[count - 1, (int)sym];
        }

        void UpdateGameboard()
        {
            for(int i = 0; i < ReelCount; i++)
            {
                // set i1,i2, i3 to consecutive slot numbers on this reel, wrap if needed
                int reelLength = reels[i].Length;
                int i1 = rand.Next(reelLength);
                int i2 = (i1 + 1) % reelLength;
                int i3 = (i2 + 1) % reelLength;

                // Populate Random Gameboard
                gameBoard[0, i] = reels[i][i1];
                gameBoard[1, i] = reels[i][i2];
                gameBoard[2, i] = reels[i][i3];
            }
        }

        void PrintGameboard()
        {
            Console.WriteLine("GAMEBOARD:");
            Console.WriteLine("------------------");
            for (int r = 0; r < GameBoardRowCount; r++)
            {
                for (int c = 0; c < ReelCount; c++)
                    Console.Write("{0,12}  ", gameBoard[r, c]);
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static readonly int [][] PayLineRows =
        {
            new [] { 1, 1, 1, 1, 1 },
            new [] { 0, 0, 0, 0, 0 },
            new [] { 2, 2, 2, 2, 2 },
            new [] { 0, 1, 2, 1, 0 },
            new [] { 2, 1, 0, 1, 2 },

            new [] { 2, 2, 1, 0, 0 },
            new [] { 0, 0, 1, 2, 2 },

            new [] { 1, 2, 1, 0, 1 },
            new [] { 1, 0, 1, 2, 1 },

            new [] { 2, 1, 1, 1, 0 },
            new [] { 0, 1, 1, 1, 2 },

            new [] { 1, 2, 2, 1, 0 },
            new [] { 1, 0, 0, 1, 2 },

            new [] { 1, 1, 2, 1, 0 },
            new [] { 1, 1, 0, 1, 2 },
        };

        IEnumerable<Symbol[]> GetPayLines()
        {
            foreach(var row in PayLineRows)
            {
                yield return new[]
                {
                    gameBoard[row[0], 0],
                    gameBoard[row[1], 1],
                    gameBoard[row[2], 2],
                    gameBoard[row[3], 3],
                    gameBoard[row[4], 4],
                };
            }
        }

        void PrintPayline(int payLineNum, in Symbol[] line, int payout)
        { 
            Console.Write("Payline[{0,2}]: [  ", payLineNum);
            foreach (var sym in line)
                Console.Write("{0,12}  ", sym);
            Console.WriteLine("]  PAYS {0} credits.", payout);
        }

        int CountScatterColumns()
        {
            // Check each column (reel) in GameBoard for Scatters
            // Scatter wins only count 1 scatter per column
            int count = 0;
            for (int c = 0; c < ReelCount; c++)
            {
                for (int r = 0; r < GameBoardRowCount; r++)
                {
                    if (gameBoard[r, c] == Symbol.Scatter)
                    {
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        int GetScatterWinCredits()
        {
            int win = 0;
            switch (CountScatterColumns())
            {
            case 3:
                win = 5;
                break;
            case 4:
                win = 25;
                break;
            case 5:
                win = 200;
                break;
            }
            return win;
        }
    }
}
