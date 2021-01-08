/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using Viu;
using Viu.Components;
using Viu.Strategy;
using Viu.Table;

namespace SCFE
{
    internal class ColorfulLabel : TextComponent
    {
        public ColorfulLabel(string s) : base(s)
        {
            InputMap.Put(new KeyStroke('c', false, false, false), "color");
            ActionMap.Put("color", (o, args) =>
            {
                var rng = new Random();
                var values = Enum.GetValues(typeof(ConsoleColor));
                Foreground = (ConsoleColor?) values.GetValue(rng.Next(values.Length));
                Print(args.Graphics);
            });
        }

        public override bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            return false;
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            // Hello!
            Console.Clear();

            // --- Select a test here. Uncomment it to launch it. ---
            // DO NOT LAUNCH TWO TESTS AT THE SAME TIME!

            // A test for Buttons (RAKHMATULLO)
            //LaunchSingleTest(TestButtons);

            // A test for TextAreas (MATHIEU)
            //LaunchSingleText(TestTextArea);

            // A BorderStrategy test
            //LaunchSingleTest(TestBorders);

            // An input handler test for quickly checking the properties of ConsoleKeyInfo objects
            //LaunchSingleTest(TestInput);

            // Test the Table component
            //LaunchSingleTest(TestTable);

            // Another Table component test but more complete
            //ViuExampleTable.LaunchExample();

            // Launch the full SCFE app (or at least its current prototype)
            TestApp(args.Length > 0 ? args[0] : null);

            // --- DEFENSE 1 DEMOS ---
            // Figures 1 and 2
            //LaunchSingleTest(TestSimpleBorder);

            // Figure 3
            //LaunchSingleTest(TestTable);

            //var viuDemo = new ViuDemo();

            while (true)
                Thread.Sleep(1000);
            // ReSharper disable once FunctionNeverReturns
        }

        // ReSharper disable once UnusedMember.Local
        private static void LaunchSingleTest(Func<Parent> test)
        {
            var cons = new ConsoleParent(test());
            cons.Validate();
            cons.FocusFirst();
            cons.Print();
        }

        public static Parent TestSimpleBorder()
        {
            var par = new Parent(new BorderStrategy());

            par.AddComponent(new TextComponent("Hey!") {VAlign = VerticalAlignment.Centered}, BorderStrategy.Left);
            par.AddComponent(
                new TextComponent("This is at the top of the window!") {HAlign = HorizontalAlignment.Centered},
                BorderStrategy.Top);
            par.AddComponent(
                new TextComponent("This is at the bottom of the window!") {HAlign = HorizontalAlignment.Centered},
                BorderStrategy.Bottom);
            par.AddComponent(new TextComponent("Bye!") {VAlign = VerticalAlignment.Centered}, BorderStrategy.Right);

            var center = new Parent(new LineStrategy {Centered = true});
            par.AddComponent(center, BorderStrategy.Center);

            center.AddComponent(new TextComponent("This is a simple demo of the 'BorderStrategy'.")
                {HAlign = HorizontalAlignment.Centered, Focusable = true});
            center.AddComponent(new TextComponent("The middle container uses a 'LineStrategy'")
                {HAlign = HorizontalAlignment.Centered, Focusable = true});
            center.AddComponent(new TextComponent("with multiple Text components on top of each other.")
                {HAlign = HorizontalAlignment.Centered, Focusable = true});

            return par;
        }

        public static Parent TestButtons()
        {
            var par = new Parent(new LineStrategy {Centered = true, Gap = 1});

            var txt = new TextComponent("I am a text! I do texty stuff!")
                {HAlign = HorizontalAlignment.Centered};

            par.AddComponent(new TextComponent(
                "Use arrow keys to move around options. " +
                "Press Enter to apply the color."));

            var btns = new Parent(new FlowStrategy(true, 1, 0));
            foreach (ConsoleColor col in Enum.GetValues(typeof(ConsoleColor)))
            {
                if (col == ConsoleColor.Black)
                    continue;

                var b = new Button("I like " + Enum.GetName(typeof(ConsoleColor), col)) {Foreground = col};
                btns.AddComponent(b);
                b.ActionOnComponent += (sender, args) =>
                {
                    txt.Foreground = col;
                    txt.Print(args.Graphics);
                };
            }

            par.AddComponent(btns);

            par.AddComponent(txt);

            return par;
        }

        public static void TestApp(string startingPath)
        {
            var colFile = File.UserHome.GetChildMaybe(".SCFE")?.GetChildMaybe("columns.txt");
            string[] columns = null;
            if (colFile != null && colFile.Exists())
            {
                using (StreamReader sr = new StreamReader(colFile.FullPath))
                {
                    var colStr = sr.ReadLine();
                    if (colStr != null)
                        columns = colStr.Split(',');
                }
            }

            if (columns == null)
                columns = new[] {"name", "git", "size", "date"};
            
            var f = new File(startingPath);
            var app = new ScfeApp(f.Exists() ? f : null, columns);
            app.Show();
        }

        public static Parent TestTable()
        {
            var par = new Parent(new BorderStrategy()) {ClearAreaBeforePrint = false};

            var data = new ObservableCollection<string[]>
            {
                new[] {"Hello!", "This is a table", "A nice one"},
                new[] {"hey", "bonjour", "buon giorno"},
                new[] {"who", "are", "you?"}
            };

            var table = new TableComponent<string[]>
                {Data = data};
            //table.AddColumn(new BasicColumnInformationType<string[]>("Test.", s => s[0]));
            //table.AddColumn(new BasicColumnInformationType<string[]>("Zboui zboui zboui.", s => s[1]));
            //table.AddColumn(new BasicColumnInformationType<string[]>("Test...", s => s[2]));
            table.AddColumn(new IndicatorColumnType<string[]>());
            table.AddColumn(new MultistateColumnType<string[]>(new[] {"This is a screenshot", "Screenshot"},
                x => new[] {x[0], x[0].Split(' ')[0], x[0].ToCharArray()[0] + ""}
            ) {Priority = 1, GrowPriority = 10});
            table.AddColumn(new MultistateColumnType<string[]>(
                new[] {"A fairly long column name", "A shorter one", "A"},
                x => new[]
                    {x[1], x[1].Split(' ')[0], x[1].ToCharArray()[0] + ""}
            ) {Priority = 2, GrowPriority = 20});
            table.AddColumn(new MultistateColumnType<string[]>(new[] {"I am running out of ideas", "Ideas", "I"},
                x => new[] {x[2], x[2].Split(' ')[0], x[2].ToCharArray()[0] + ""}
            ) {Priority = 2, GrowPriority = 10});
            par.AddComponent(table, BorderStrategy.Center);

            return par;
        }

        public static void LaunchTestInput()
        {
            ConsoleKeyInfo cki;
            // Prevent example from ending if CTL+C is pressed.
            Console.TreatControlCAsInput = true;

            Console.WriteLine("Press any combination of CTL, ALT, and SHIFT, and a console key.");
            Console.WriteLine("Press the Escape (Esc) key to quit: \n");
            do
            {
                cki = Console.ReadKey();
                Console.Write(" --- You pressed ");
                if ((cki.Modifiers & ConsoleModifiers.Alt) != 0) Console.Write("ALT+");
                if ((cki.Modifiers & ConsoleModifiers.Shift) != 0) Console.Write("SHIFT+");
                if ((cki.Modifiers & ConsoleModifiers.Control) != 0) Console.Write("CTL+");
                Console.WriteLine(cki.KeyChar + " (isletter: " + (cki.KeyChar != (char) 0) + ")");
            } while (cki.Key != ConsoleKey.Escape);
        }

        public static Parent TestTextArea()
        {
            var parent = new Parent(new LineStrategy {Gap = 1});

            var tf = new TextField();
            parent.AddComponent(new BoxContainer(tf));

            var tc = new TextComponent {HAlign = HorizontalAlignment.Centered};
            var p = new Parent(new BorderStrategy()) {ClearAreaBeforePrint = true};
            p.AddComponent(tc, BorderStrategy.Center);
            parent.AddComponent(p);

            tf.ActionOnComponent += (sender, args) =>
            {
                tc.Text = tf.Text;
                p.Print(args.Graphics);
            };

            return parent;
        }

        public static Parent TestFlow()
        {
            var parent = new Parent(new FlowStrategy(1));

            var text = "This is a test of the flow layout! How amazing".Split(' ');
            foreach (var s in text)
            {
                var tc = new TextComponent(s) {Focusable = true};
                parent.AddComponent(tc);
            }

            return parent;
        }

        public static Parent TestBorders()
        {
            var parent = new Parent(new BorderStrategy());

            TextComponent txt = new ColorfulLabel("HELLO!")
            {
                VAlign = VerticalAlignment.Centered,
                HAlign = HorizontalAlignment.Centered, Focusable = true
            };
            parent.AddComponent(new BoxContainer(txt), BorderStrategy.Center);

            var p2 = new Parent(new FlowStrategy(1));
            var text = "Flow Lay".Split(' ');
            foreach (var s in text)
            {
                var tc = new TextComponent(s) {Focusable = true};
                p2.AddComponent(tc);
            }

            parent.AddComponent(p2, BorderStrategy.Left);

            TextComponent txtr = new ColorfulLabel("Right!") {VAlign = VerticalAlignment.Centered, Focusable = true};
            parent.AddComponent(txtr, BorderStrategy.Right);

            p2 = new Parent(new FlowStrategy(true, 1, 0));
            text = "This is a test of the flow layout! How amazing".Split(' ');
            foreach (var s in text)
            {
                var tc = new TextComponent(s) {Focusable = true};
                p2.AddComponent(tc);
            }

            parent.AddComponent(p2, BorderStrategy.Top);

            TextComponent txtb = new ColorfulLabel("Down!") {HAlign = HorizontalAlignment.Centered, Focusable = true};
            parent.AddComponent(txtb, BorderStrategy.Bottom);

            return parent;
        }
    }
}
