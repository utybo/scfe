/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 * 
 * This Source Code Form is "Incompatible With Secondary Licenses", as
 * defined by the Mozilla Public License, v. 2.0.
 */
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;

namespace Viu.Components
{
    /// <inheritdoc cref="Container" />
    /// <inheritdoc cref="IFocusable" />
    /// <summary>
    ///     A ConsoleParent is a "root parent" which provides basic bindings of the various properties of the console.
    ///     <para>
    ///         The creation of a ConsoleParent should look like this:
    ///         <pre>
    ///             <code>
    /// Parent p = new Parent();
    /// // Create everything...
    /// ConsoleParent cons = new ConsoleParent(p);
    /// cons.Validate();
    /// cons.Print();
    /// </code>
    ///         </pre>
    ///         At any given time a ConsoleParent has two threads running:
    ///         * A thread which listens for key inputs and propagates them to the IFocusable architecture of this component
    ///         * A thread which watches the changes in the size of the console and applies these changes to the ConsoleParent
    ///     </para>
    /// </summary>
    /// <remarks>This is a root parent, meaning that it provides a graphical context for all other components.</remarks>
    public class ConsoleParent : Container, IFocusable, IEventThreadManager
    {
        private readonly ConcurrentQueue<Action<GraphicsContext>> _eventQueue =
            new ConcurrentQueue<Action<GraphicsContext>>();

        private Thread _watcher, _input, _eventThread;

        /// <summary>
        ///     Create and initialize a new ConsoleParent, using the given parent as its first component. The given parent
        ///     will have its X and Y coordinates set to 0, and its height and width set and progressively resized to the
        ///     dimensions of the Console.
        /// </summary>
        /// <param name="root"></param>
        public ConsoleParent(Parent root)
        {
            SetRootContainer(root);
            Initialize();
        }

        public ConsoleGraphicsContext GraphicsContext { get; } = new ConsoleGraphicsContext();

        public Action<Exception, GraphicsContext> ExceptionHandler { get; set; }

        public void DoGraphicsLater(Action<GraphicsContext> action)
        {
            _eventQueue.Enqueue(action);
        }

        public void DoGraphicsAndWait(Action<GraphicsContext> action)
        {
            CountdownEvent cde = new CountdownEvent(1);
            _eventQueue.Enqueue(g =>
            {
                try
                {
                    action(g);
                }
                finally
                {
                    cde.Signal();
                }
            });
            cde.Wait();
        }

        public bool AcceptInput(ConsoleKeyInfo keyPressed, GraphicsContext g)
        {
            return (Components[0] as IFocusable)?.AcceptInput(keyPressed, g) ?? false;
        }

        public bool IsFocusable()
        {
            return (Components[0] as IFocusable)?.IsFocusable() ?? false;
        }

        public void SetFocused(bool focused, GraphicsContext g)
        {
            (Components[0] as IFocusable)?.SetFocused(focused, g);
        }

        public bool IsFocused()
        {
            return (Components[0] as IFocusable)?.IsFocused() ?? false;
        }

        /// <summary>
        ///     Initialize the ConsoleParent, launching the threads and creating the default values in the InputMap
        /// </summary>
        private void Initialize()
        {
            GraphicsContext.Initialize();
            _watcher = new Thread(WatchConsoleSizeChanges) {IsBackground = true};
            _watcher.Start();
            _input = new Thread(WatchInputs) {IsBackground = true};
            _input.Start();
            _eventThread = new Thread(WatchEvents) {IsBackground = true};
            _eventThread.Start();

            // Initialize basic keys
            InputMap.Put(new KeyStroke(ConsoleKey.UpArrow, false, false, false), StandardActionNames.MoveUp);
            InputMap.Put(new KeyStroke(ConsoleKey.LeftArrow, false, false, false), StandardActionNames.MoveLeft);
            InputMap.Put(new KeyStroke(ConsoleKey.RightArrow, false, false, false), StandardActionNames.MoveRight);
            InputMap.Put(new KeyStroke(ConsoleKey.DownArrow, false, false, false), StandardActionNames.MoveDown);
            InputMap.Put(new KeyStroke(ConsoleKey.Enter, false, false, false), StandardActionNames.BaseAction);
            InputMap.Put(new KeyStroke(ConsoleKey.Enter, false, false, true), StandardActionNames.SecondaryAction);

            InputMap.Put(new KeyStroke(ConsoleKey.Backspace, false, false, false),
                StandardActionNames.DeleteToTheLeftAction);
            InputMap.Put(new KeyStroke(ConsoleKey.Backspace, true, false, false),
                StandardActionNames.DeleteWordToTheLeftAction);
            InputMap.Put(new KeyStroke(ConsoleKey.Delete, false, false, false),
                StandardActionNames.DeleteToTheRightAction);
            InputMap.Put(new KeyStroke(ConsoleKey.Delete, true, false, false),
                StandardActionNames.DeleteWordToTheRightAction);
            InputMap.Put(new KeyStroke(ConsoleKey.Home, false, false, false), StandardActionNames.MoveLineStart);
            InputMap.Put(new KeyStroke(ConsoleKey.End, false, false, false), StandardActionNames.MoveLineEnd);
            InputMap.Put(new KeyStroke(ConsoleKey.Escape, false, false, false), StandardActionNames.CancelAction);


            InputMap.Put(new KeyStroke(ConsoleKey.LeftArrow, true, false, false), StandardActionNames.MoveLeftWord);
            InputMap.Put(new KeyStroke(ConsoleKey.RightArrow, true, false, false), StandardActionNames.MoveRightWord);
        }

        private void WatchEvents()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                while (_eventQueue.Count > 0)
                {
                    _eventQueue.TryDequeue(out var act);

                    try
                    {
                        act?.Invoke(GraphicsContext);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            ExceptionHandler?.Invoke(e, GraphicsContext);
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                Thread.Sleep(5);
            }
        }

        private void WatchInputs()
        {
            var usesV2 = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName
                             ?.Contains("v2") ?? false;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                while (Thread.CurrentThread.IsAlive)
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);

                        var f = Components[0] as IFocusable;
                        DoGraphicsLater(g =>
                        {
                            f?.AcceptInput(key, g);
                            if (usesV2)
                            {
                                g.SetCursorVisible(false);
                                Print(g);
                                ((Components[0] as Parent)?.GetFocusedElement(true) as ICursorFocusable)
                                    ?.UpdateCursorState(g);
                            }
                        });
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
            else
                while (Thread.CurrentThread.IsAlive)
                {
                    var key = Console.ReadKey(true);

                    var f = Components[0] as IFocusable;
                    DoGraphicsLater(g => { f?.AcceptInput(key, g); });
                }
        }

        private void WatchConsoleSizeChanges()
        {
            int w = Console.WindowWidth, h = Console.WindowHeight;
            long lastResizeTime = 0;
            var needsReprint = false;
            while (Thread.CurrentThread.IsAlive)
                try
                {
                    if (needsReprint && lastResizeTime + 100 <= DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    {
                        Console.Clear();
                        Validate();
                        Print(GraphicsContext);
                        needsReprint = false;
                    }

                    Thread.Sleep(20);
                    if (Console.WindowWidth == w && Console.WindowHeight == h)
                        continue;
                    Console.Clear();
                    Console.CursorVisible = false;

                    w = Console.WindowWidth;
                    h = Console.WindowHeight;
                    var s = "Width: " + w + " | Height: " + h;
                    var x = w / 2 - s.Length / 2;
                    var y = h / 2;
                    Console.SetCursorPosition(x < 0 ? 0 : x, y);
                    Console.Write(s);
                    needsReprint = true;
                    lastResizeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
                catch (Exception)
                {
                    //ignored
                }
        }

        /// <summary>
        ///     Replace the main container of this ConsoleParent by the given one
        /// </summary>
        /// <param name="p">The new parent to use</param>
        public void SetRootContainer(Parent p)
        {
            Components.Clear();
            Components.Add(p);
            p.Parent = this;
        }

        public override Dimensions ComputeDimensions()
        {
            return new Dimensions(Console.WindowWidth, Console.WindowHeight);
        }

        public override void Validate()
        {
            var d = ComputeDimensions();
            Width = d.Width;
            Height = d.Height;

            if (Components.Count <= 0)
                return;
            var cont = Components[0] as Container;
            Debug.Assert(cont != null, nameof(cont) + " != null");
            cont.Height = Height;
            cont.Width = Width;
            cont.X = 0;
            cont.Y = 0;
            cont.Validate();
        }

        public void Print()
        {
            Print(GraphicsContext);
        }

        [Obsolete("This method should only be used as a last resort.")]
        protected override GraphicsContext GetGraphicsContext()
        {
            return GraphicsContext;
        }

        /// <summary>
        ///     Focus the first component of the tree by focusing the root parent of this ConsoleParent
        /// </summary>
        public void FocusFirst()
        {
            var p = Components[0] as Parent;
            p?.SetFocused(true, GraphicsContext);
        }

        /// <summary>
        ///     Set the title of the console to the given string
        /// </summary>
        /// <param name="title">The new title of the console</param>
        public void SetTitle(string title)
        {
            Console.Title = title;
        }

        public override IEventThreadManager GetEventThread()
        {
            return this;
        }
    }
}
