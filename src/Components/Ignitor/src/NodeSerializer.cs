// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

#nullable enable

namespace Ignitor;

internal static class NodeSerializer
{
    public static string Serialize(ElementHive hive)
    {
        using (var writer = new StringWriter())
        {
            var serializer = new Serializer(writer);
            serializer.SerializeHive(hive);
            return writer.ToString();
        }
    }

    public static string Serialize(Node node)
    {
        using (var writer = new StringWriter())
        {
            var serializer = new Serializer(writer);
            serializer.Serialize(node);
            return writer.ToString();
        }
    }

    private class Serializer
    {
        private readonly TextWriter _writer;
        private int _depth;
        private bool _atStartOfLine;
        private readonly HashSet<Node> _visited = new HashSet<Node>();

        public Serializer(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void SerializeHive(ElementHive hive)
        {
            foreach (var kvp in hive.Components)
            {
                Serialize(kvp.Value);
            }
        }

        public void Serialize(Node node)
        {
            if (!_visited.Add(node))
            {
                // This is a child component of a previously seen component. Don't repeat it
                return;
            }

            switch (node)
            {
                case ComponentNode componentNode:
                    {
                        SerializeComponent(componentNode);
                        break;
                    }
                case ElementNode elementNode:
                    {
                        SerializeElement(elementNode);
                        break;
                    }
                case TextNode textNode:
                    {
                        SerializeTextNode(textNode);
                        break;
                    }
                case MarkupNode markupNode:
                    {
                        SerializeMarkupNode(markupNode);
                        break;
                    }
                case ContainerNode containerNode:
                    {
                        SerializeChildren(containerNode);
                        break;
                    }
                default:
                    {
                        Write("--- UNKNOWN (");
                        Write(node.GetType().ToString());
                        WriteLine(") ---");
                        break;
                    }
            }
        }

        private void SerializeMarkupNode(MarkupNode markupNode)
        {
            Write("M: ");
            WriteLine(markupNode.MarkupContent.Replace(Environment.NewLine, "\\r\\n"));
        }

        private void SerializeTextNode(TextNode textNode)
        {
            Write("T: ");
            WriteLine(textNode.TextContent);
        }

        private void SerializeElement(ElementNode elementNode)
        {
            Write("<");
            Write(elementNode.TagName);

            foreach (var attribute in elementNode.Attributes)
            {
                Write(" ");
                Write(attribute.Key);

                if (attribute.Value != null)
                {
                    Write("=\"");
                    Write(attribute.Value.ToString()!);
                    Write("\"");
                }
            }

            if (elementNode.Properties.Count > 0)
            {
                Write("  Properties: [");

                foreach (var properties in elementNode.Properties)
                {
                    Write(" ");
                    Write(properties.Key);

                    if (properties.Value != null)
                    {
                        Write("=\"");
                        Write(properties.Value.ToString()!);
                        Write("\"");
                    }
                }
                Write("]");
            }

            if (elementNode.Events.Count > 0)
            {
                Write("  Events: [");

                foreach (var evt in elementNode.Events)
                {
                    Write(" ");
                    Write(evt.Value.EventName);
                    Write("(");
                    Write(evt.Value.EventId.ToString(CultureInfo.InvariantCulture));
                    Write(")");
                }
                Write("]");
            }

            WriteLine(">");

            _depth++;
            SerializeChildren(elementNode);
            _depth--;
            Write("</");
            Write(elementNode.TagName);
            WriteLine("/>");
        }

        private void SerializeChildren(ContainerNode containerNode)
        {
            for (var i = 0; i < containerNode.Children.Count; i++)
            {
                Serialize(containerNode.Children[i]);
            }
        }

        private void SerializeComponent(ComponentNode component)
        {
            Write("[Component ( ");
            Write(component.ComponentId.ToString(CultureInfo.InvariantCulture));
            WriteLine(" )]");
            _depth++;
            SerializeChildren(component);
            _depth--;
        }

        private void Write(string content)
        {
            if (_atStartOfLine)
            {
                WriteIndent();
            }

            _writer.Write(content);

            _atStartOfLine = false;
        }

        private void WriteLine(string content)
        {
            if (_atStartOfLine)
            {
                WriteIndent();
            }

            _writer.WriteLine(content);
            _atStartOfLine = true;
        }

        private void WriteIndent()
        {
            var indent = new string(' ', _depth * 4);
            _writer.Write(indent);
        }
    }
}
#nullable restore
