﻿using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using static Transgenesis.Global;
using ColoredString = SadConsole.ColoredString;
namespace Transgenesis {
    class ElementEditor : IComponent {
        ProgramState state;
        Stack<IComponent> screens;
        Environment env;
        TranscendenceExtension extension;
        ConsoleManager c;

        XElement focused;
        HashSet<XElement> keepExpanded;

        Input i;
        History h;
        Suggest s;
        Tooltip t;
        Scroller scroller;

        public ElementEditor(ProgramState state, Stack<IComponent> screens, Environment env, TranscendenceExtension extension, ConsoleManager c) {
            this.state = state;
            this.screens = screens;
            this.env = env;
            this.extension = extension;
            this.focused = extension.structure;
            this.keepExpanded = new HashSet<XElement>();
            this.c = c;
            i = new Input(c);
            h = new History(i);
            s = new Suggest(i, c);
            t = new Tooltip(i, s, c, new Dictionary<string, string>() {
                {"",    "Navigate Mode" + "\r\n" +
                        "-Up          Previous element" + "\r\n" +
                        "-Down        Next element" + "\r\n" +
                        "-Ctrl+Up     Move element up" + "\r\n" +
                        "-Ctrl+Down   Move element down" + "\r\n" +
                        "-Left        Parent element" + "\r\n" +
                        "-Right       First child element" + "\r\n" +
                        "-Return      Expand/collapse element" + "\r\n" +
                        "-Typing      Enter a command" + "\r\n"},
                {"add", "add <subelement>\r\n" +
                        "Adds the named subelement to the current element, if allowed"},
                {"reorder", "reorder <attribute...>\r\n" +
                        "Reorders the attributes in the current element in the specified order" },
                {"set", "set <attribute> [value]\r\n" +
                        "Sets the named attribute to the specified value on the current element. If [value] is empty, then deletes the attribute from the element" },

                //{"remove", "remove <subelement>\r\n" +
                //        "Removes the named subelement from the current element"},
                {"remove", "remove\r\n" +
                        "Removes the current element from its parent, if allowed"},
                {"bind", "bind\r\n" +
                        "Updates type bindings for the current extension"},
                {"bindall", "bindall\r\n" +
                        "Updates type bindings for all loaded extensions"},
                {"save", "save\r\n" +
                        "Saves the current extension to its file"},
                {"saveall", "saveall\r\n" +
                        "Saves all loaded extensions to their files"},
                {"expand", "expand\r\n" +
                        "Expands the current element to display all of its attributes and children"},
                {"collapse", "collapse\r\n" +
                        "Collapses the current element to hide most of its attributes and children"},
                {"moveup", "moveup\r\n" +
                        "Moves the current element up in its parent's order of children"},
                {"movedown", "movedown\r\n" +
                        "Moves the current element down in its parent's order of children"},
                {"root", "root\r\n" +
                        "Selects the root as the current element"},
                {"parent", "parent\r\n" +
                        "Selects the parent of the current element"},
                {"next", "next\r\n" +
                        "Selects the next child of the current element's parent"},
                {"previous", "previous\r\n" +
                        "Selects the previous child of the current element's parent"},
                {"types", "types\r\n" +
                        "Opens the Type Editor on this extension"},
                {"exit", "exit\r\n" +
                        "Exits this XML Editor and returns to the main menu"},
            });
            scroller = new Scroller(i, c);
            //{"", () => new List<string>{ "set", "add", "remove", "bind", "bindall", "save", "saveall", "moveup", "movedown", "root", "parent", "next", "prev", "types", "exit" } },
        }
        public void Draw() {
            c.Clear();
            c.SetCursor(new Point(0, 0));
            //Console.WriteLine(extension.structure.ToString());

            HashSet<XElement> expanded = new HashSet<XElement>(keepExpanded);

            const bool expandFocusedPath = true;
            if(expandFocusedPath) {
                var f = focused;
                while (f != null) {
                    expanded.Add(f);
                    f = f.Parent;
                }
            }

            HashSet<XElement> semiexpanded = new HashSet<XElement>();

            //Add this element and its ancestors to the semiexpanded list
            foreach(var expandedElement in keepExpanded) {
                MarkAncestorsSemiExpanded(expandedElement);
            }
            MarkAncestorsSemiExpanded(focused);

            //We auto-expand children of the focused element if we press Ctrl-F
            //MarkDescendantsSemiExpanded(focused);
            void MarkAncestorsSemiExpanded(XElement element) {
                while (element != null) {
                    if (!semiexpanded.Contains(element)) {
                        semiexpanded.Add(element);
                        element = element.Parent;
                    } else {
                        //If we've already marked this element, then we have also marked its ancestors
                        break;
                    }
                }
            }
            void MarkDescendantsSemiExpanded(XElement parent) {
                Queue<XElement> elements = new Queue<XElement>(parent.Elements());
                while(elements.Count > 0) {
                    var e = elements.Dequeue();
                    semiexpanded.Add(e);
                    foreach(var child in e.Elements()) {
                        elements.Enqueue(child);
                    }
                }
            }

            var root = focused;
            while(root.Parent != null) {
                root = root.Parent;
            }

            var formatter = new ElementFormatter(c);
            formatter.ShowElementTree(root, focused, expanded, semiexpanded);
            formatter.SyntaxHighlight();
            List<ColoredString> buffer = formatter.buffer;
            /*
            {
                List<ColoredString> buffer2 = new List<ColoredString>();
                ColoredString splitline = new ColoredString(150);
                int index = 0;
                foreach (var line in buffer) {
                    foreach (var c in line) {
                        if (c.Glyph == '\n') {
                            buffer2.Add(splitline);
                            splitline = new ColoredString(150);
                            index = 0;
                        } else {
                            splitline[index] = c;
                            index++;
                            if (index == 150) {
                                buffer2.Add(splitline);
                                splitline = new ColoredString(150);
                                index = 0;
                            }
                        }
                    }
                    if(index > 0) {
                        buffer2.Add(splitline);
                        splitline = new ColoredString(150);
                        index = 0;
                    }
                }
                buffer = buffer2;
            }
            */

            scroller.Draw(buffer);

            i.Draw();
            s.Draw();
            t.Draw();
        }

        public void Handle(ConsoleKeyInfo k) {
            i.Handle(k);
            h.Handle(k);
            s.Handle(k);
            scroller.Handle(k);

            string input = i.Text;
            switch (k.Key) {
                /*
                case ConsoleKey.LeftArrow when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    focused = focused.Parent ?? focused;
                    break;
                case ConsoleKey.RightArrow when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    focused = focused.Elements().FirstOrDefault() ?? focused;
                    break;
                case ConsoleKey.OemPlus when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    focused = focused.ElementsAfterSelf().FirstOrDefault() ?? focused;
                    break;
                case ConsoleKey.OemMinus when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    focused = focused.ElementsBeforeSelf().LastOrDefault() ?? focused;
                    break;
                    */

                case ConsoleKey.R when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    //Removes the current element
                    RemoveFocused();
                    break;
                case ConsoleKey.D when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    //Make a duplicate of the element
                    var duplicate = new XElement(focused);
                    //Remember to copy the base template so that we know how to treat this element
                    env.bases[duplicate] = env.bases[focused];
                    focused.AddAfterSelf(duplicate);
                    break;
                case ConsoleKey.C when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    //Remember this element
                    state.copied = focused;
                    break;
                case ConsoleKey.V when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    //Later, we should attempt to manually reconstruct the element as allowed by the parent's template

                    //Paste a deep copy of the element
                    if (state.copied != null) {
                        var copy = new XElement(state.copied);
                        //Remember to copy the base template so that we know how to treat this element
                        env.bases[copy] = env.bases[state.copied];
                        focused.Add(copy);
                    }
                    break;
                case ConsoleKey.DownArrow when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    MoveDown();
                    break;
                case ConsoleKey.UpArrow when (k.Modifiers & ConsoleModifiers.Control) != 0:
                    MoveUp();
                    break;

                //Navigate using arrow keys when command input is empty
                case ConsoleKey.LeftArrow when i.Text.Length == 0:
                    focused = focused.Parent ?? focused;
                    break;
                case ConsoleKey.RightArrow when i.Text.Length == 0:
                    focused = focused.Elements().FirstOrDefault() ?? focused;
                    break;
                case ConsoleKey.DownArrow when i.Text.Length == 0:
                    focused = focused.ElementsAfterSelf().FirstOrDefault() ?? focused;
                    break;
                case ConsoleKey.UpArrow when i.Text.Length == 0:
                    focused = focused.ElementsBeforeSelf().LastOrDefault() ?? focused;
                    break;
                case ConsoleKey.Enter: {
                        if(input.Length == 0) {
                            if(keepExpanded.Contains(focused)) {
                                keepExpanded.Remove(focused);
                            } else {
                                keepExpanded.Add(focused);
                            }

                            break;
                        }

                        string[] parts = input.Split(' ');
                        switch (parts[0]) {
                            case "add": {
                                    string elementName = parts[1];
                                    if (env.CanAddElement(focused, env.bases[focused], elementName, out XElement subtemplate)) {
                                        var subelement = env.FromTemplate(subtemplate, elementName);
                                        focused.Add(subelement);
                                        h.Record();
                                    }
                                    break;
                                }
                            case "set": {
                                    if (parts.Length == 1)
                                        break;
                                    string attribute = parts[1];
                                    string value = string.Join(" ", parts.Skip(2));
                                    if (value.Length > 0) {
                                        //Set the value
                                        focused.SetAttributeValue(attribute, value);
                                        h.Record();
                                    } else if (!string.IsNullOrEmpty(attribute)) {
                                        //Delete the attribute if we enter no value
                                        focused.Attribute(attribute)?.Remove();
                                        h.Record();
                                    }
                                    break;
                                }
                            case "reorder": {
                                    Dictionary<string, string> attributes = new Dictionary<string, string>();
                                    foreach(var a in focused.Attributes()) {
                                        attributes[a.Name.LocalName] = a.Value;
                                    }
                                    focused.RemoveAttributes();
                                    foreach(var a in parts.Skip(1)) {
                                        if(attributes.TryGetValue(a, out string value)) {
                                            focused.SetAttributeValue(a, value);
                                            attributes.Remove(a);
                                        }
                                    }
                                    foreach(var a in attributes.Keys) {
                                        focused.SetAttributeValue(a, attributes[a]);
                                    }
                                    h.Record();
                                    break;
                                }
                            case "bind": {
                                    extension.updateTypeBindings(env);
                                    h.Record();
                                    break;
                                }
                            case "bindall": {
                                    foreach (var ext in env.extensions.Values) {
                                        ext.updateTypeBindings(env);
                                    }
                                    h.Record();
                                    break;
                                }
                            case "save": {
                                    extension.Save();
                                    h.Record();
                                    break;
                                }
                            case "saveall": {
                                    foreach (var extension in env.extensions.Values) {
                                        extension.Save();
                                    }
                                    h.Record();
                                    break;
                                }
                            case "remove": {
                                    //TO DO

                                    //For now, this just removes the current element if it's not the root
                                    RemoveFocused();
                                    h.Record();
                                    break;
                                }
                            case "expand": {
                                    keepExpanded.Add(focused);
                                    h.Record();
                                    break;
                                }
                            case "collapse": {
                                    keepExpanded.Remove(focused);
                                    h.Record();
                                    break;
                                }
                            case "moveup": {
                                    MoveUp();
                                    h.Record();
                                    break;
                                }
                            case "movedown": {
                                    MoveDown();
                                    h.Record();
                                    break;
                                }
                            case "goto": {
                                    //Go to the specified element
                                    break;
                                }
                            case "root": {
                                    while (focused.Parent != null) {
                                        focused = focused.Parent;
                                    }
                                    h.Record();
                                    break;
                                }
                            case "parent": {
                                    focused = focused.Parent ?? focused;
                                    h.Record();
                                    break;
                                }
                                /*
                            case "child":
                                focused = focused.Elements().FirstOrDefault() ?? focused;
                                break;
                            */
                            /*
                            case "find":
                                //Start from the focused element and find elements matching this criteria
                                break;
                            */
                            case "next": {
                                    focused = focused.ElementsAfterSelf().FirstOrDefault() ?? focused;
                                    h.Record();

                                    break;
                                }
                            case "prev": {
                                    focused = focused.ElementsBeforeSelf().LastOrDefault() ?? focused;
                                    h.Record();
                                    break;
                                }
                            case "types": {
                                    screens.Push(new TypeEditor(screens, env, extension, c));
                                    h.Record();
                                    break;
                                }
                            case "exit": {
                                    screens.Pop();
                                    h.Record();
                                    break;
                                }
                        }
                        break;
                    }
                //Allow Suggest/History to handle up/down arrows
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                    break;
                default: {

                        string[] parts = input.Split(' ');
                        if (parts.Length > 2) {
                            switch (parts[0]) {
                                case "set": {
                                        //Suggest values for the attribute
                                        string attribute = parts[1];
                                        string attributeType = env.bases[focused].Elements("A").FirstOrDefault(e => e.Att("name") == attribute)?.Att("valueType");
                                        if(attributeType == null) {
                                            s.Clear();
                                            break;
                                        }
                                        var all = env.GetAttributeValues(extension, attributeType);
                                        if (focused.Att(attribute, out string value)) {
                                            //Remove duplicate
                                            all.Remove(value);
                                            //Insert at the front
                                            all.Insert(0, value);
                                        }
                                        string rest = string.Join(" ", parts.Skip(2));
                                        var items = Global.GetSuggestions(rest, all);
                                        s.SetItems(items);
                                        break;
                                    }
                            }
                        } else {
                            //Disable suggest when input is completely empty so that we can navigate aroung the UI with arrow keys
                            if (input.Length == 0) {
                                s.SetItems(new List<HighlightEntry>());
                                break;
                            }

                            var empty = new List<string>();
                            Dictionary<string, Func<List<string>>> autocomplete = new Dictionary<string, Func<List<string>>> {
                                {"", () => new List<string>{ "set", "add", "remove", "bind", "bindall", "save", "saveall", "expand", "collapse", "moveup", "movedown", "root", "parent", "next", "prev", "types", "exit" } },
                                {"set", () => env.bases[focused].GetValidAttributes() },
                                {"add", () => env.GetAddableElements(focused, env.bases[focused]) },
                                {"remove", () => env.GetRemovableElements(focused, env.bases[focused]) },
                                //bind
                                //bindall
                                //save
                                //saveall
                                //moveup
                                //movedown
                                //root
                                //parent
                                //next
                                //prev
                                //types
                                //exit
                            };
                            string p = autocomplete.Keys.Last(prefix => input.StartsWith((prefix + " ").TrimStart()));
                            List<string> all = autocomplete[p]();

                            var items = Global.GetSuggestions(input.Substring(p.Length).TrimStart(), all);
                            s.SetItems(items);
                        }
                        break;
                    }
            }
            t.Handle(k);

            /*
            if(str.Length > 0) {
                List<string> all = null;
                switch (str[0]) {
                    case char c when c >= 'a' && c <= 'z':
                        all = env.bases[focused].GetValidAttributes();
                        break;
                    case char c when c >= 'A' && c <= 'Z':
                        all = env.bases[focused].GetValidSubelements();
                        break;
                    case '&':

                        break;
                    default:
                        all = new List<string>();
                        break;
                }
                var items = Global.GetSuggestions(str, all);
                s.SetItems(items);
            }
            */
            void RemoveFocused() {
                var parent = focused.Parent;
                if (parent != null && Environment.CanRemoveElement(parent, env.bases[focused])) {
                    var before = focused.ElementsBeforeSelf().LastOrDefault();
                    focused.Remove();
                    //focused = parent;
                    focused = before ?? parent;
                }
            }
            void MoveUp() {
                var before = focused.ElementsBeforeSelf().LastOrDefault();
                if (before != null) {
                    focused.Remove();
                    before.AddBeforeSelf(focused);
                }
            }
            void MoveDown() {
                var after = focused.ElementsAfterSelf().FirstOrDefault();
                if (after != null) {
                    focused.Remove();
                    after.AddAfterSelf(focused);
                }
            }
        }

        public void Update() {
        }
    }
}
