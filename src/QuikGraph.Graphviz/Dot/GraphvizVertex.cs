using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using static QuikGraph.Utils.MathUtils;

namespace QuikGraph.Graphviz.Dot
{
    /// <summary>
    /// Graphviz vertex.
    /// </summary>
    public class GraphvizVertex
    {
        /// <summary>
        /// Position.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:pos">See more</see>
        /// </summary>
        public GraphvizPoint Position { get; set; }

        /// <summary>
        /// Comment.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:comment">See more</see>
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Label.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:label">See more</see>
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Top label.
        /// </summary>
        public string TopLabel { get; set; }

        /// <summary>
        /// Bottom label.
        /// </summary>
        public string BottomLabel { get; set; }

        /// <summary>
        /// Tooltip.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:tooltip">See more</see>
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// URL.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:URL">See more</see>
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Distortion.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:distortion">See more</see>
        /// </summary>
        public double Distortion { get; set; }

        /// <summary>
        /// Filling color.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:fillcolor">See more</see>
        /// </summary>
        public GraphvizColor FillColor { get; set; } = GraphvizColor.White;

        /// <summary>
        /// Font.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:fontname">See more</see> or
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:fontsize">See more</see>
        /// </summary>
        public GraphvizFont Font { get; set; }

        /// <summary>
        /// Font color.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:fontcolor">See more</see>
        /// </summary>
        public GraphvizColor FontColor { get; set; } = GraphvizColor.Black;

        /// <summary>
        /// Vertex group.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:group">See more</see>
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Layer.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:layer">See more</see>
        /// </summary>
        public GraphvizLayer Layer { get; set; }

        /// <summary>
        /// Orientation.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:orientation">See more</see>
        /// </summary>
        public double Orientation { get; set; }

        /// <summary>
        /// Peripheries.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:peripheries">See more</see>
        /// </summary>
        public int Peripheries { get; set; } = -1;

        /// <summary>
        /// Indicates if is regular vertex.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:regular">See more</see>
        /// </summary>
        public bool Regular { get; set; }

        /// <summary>
        /// Record info.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:label">See more</see>
        /// </summary>
        public GraphvizRecord Record { get; set; } = new GraphvizRecord();

        /// <summary>
        /// Vertex shape.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:shape">See more</see>
        /// </summary>
        public GraphvizVertexShape Shape { get; set; } = GraphvizVertexShape.Unspecified;

        /// <summary>
        /// Vertex sides.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:sides">See more</see>
        /// </summary>
        public int Sides { get; set; } = 4;

        /// <summary>
        /// Vertex size.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:width">See more</see> or
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:height">See more</see>
        /// </summary>
        public GraphvizSizeF Size { get; set; } = new GraphvizSizeF(0f, 0f);

        /// <summary>
        /// Fixed size.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:fixedsize">See more</see>
        /// </summary>
        public bool FixedSize { get; set; }

        /// <summary>
        /// Skew.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:skew">See more</see>
        /// </summary>
        public double Skew { get; set; }

        /// <summary>
        /// Stroke color.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:color">See more</see>
        /// </summary>
        public GraphvizColor StrokeColor { get; set; } = GraphvizColor.Black;

        /// <summary>
        /// Vertex style.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:style">See more</see>
        /// </summary>
        public GraphvizVertexStyle Style { get; set; } = GraphvizVertexStyle.Unspecified;

        /// <summary>
        /// Z index.
        /// <see href="https://www.graphviz.org/doc/info/attrs.html#d:z">See more</see>
        /// </summary>
        public double Z { get; set; } = -1;

        [Pure]
        [NotNull]
        internal string GenerateDot([NotNull] Dictionary<string, object> properties)
        {
            using (var writer = new StringWriter())
            {
                bool flag = false;
                foreach (KeyValuePair<string, object> pair in properties)
                {
                    if (flag)
                    {
                        writer.Write(", ");
                    }
                    else
                    {
                        flag = true;
                    }

                    switch (pair.Value)
                    {
                        case string strValue:
                            writer.Write($"{pair.Key}=\"{strValue}\"");
                            continue;

                        case GraphvizVertexShape shape:
                            writer.Write($"{pair.Key}={shape.ToString().ToLower()}");
                            continue;

                        case GraphvizVertexStyle style:
                            writer.Write($"{pair.Key}={style.ToString().ToLower()}");
                            continue;

                        case GraphvizColor color:
                        {
                            writer.Write(
                                "{0}=\"#{1}{2}{3}{4}\"",
                                pair.Key,
                                color.R.ToString("x2").ToUpper(),
                                color.G.ToString("x2").ToUpper(),
                                color.B.ToString("x2").ToUpper(),
                                color.A.ToString("x2").ToUpper());
                            continue;
                        }

                        case GraphvizRecord record:
                            writer.WriteLine($"{pair.Key}=\"{record.ToDot()}\"");
                            continue;

                        default:
                            writer.Write($" {pair.Key}={pair.Value.ToString().ToLower()}");
                            break;
                    }
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Converts this vertex to DOT.
        /// </summary>
        /// <returns>Vertex as DOT.</returns>
        [Pure]
        [NotNull]
        public string ToDot()
        {
            var properties = new Dictionary<string, object>();
            if (Font != null)
            {
                properties["fontname"] = Font.Name;
                properties["fontsize"] = Font.SizeInPoints;
            }
            if (FontColor != GraphvizColor.Black)
            {
                properties["fontcolor"] = FontColor;
            }
            if (Shape != GraphvizVertexShape.Unspecified)
            {
                properties["shape"] = Shape;
            }
            if (Style != GraphvizVertexStyle.Unspecified)
            {
                properties["style"] = Style;
            }
            if (Shape == GraphvizVertexShape.Record)
            {
                properties["label"] = Record;
            }
            else if (Label != null)
            {
                properties["label"] = Label;
            }
            if (FixedSize)
            {
                properties["fixedsize"] = true;
                if (Size.Height > 0f)
                {
                    properties["height"] = Size.Height;
                }
                if (Size.Width > 0f)
                {
                    properties["width"] = Size.Width;
                }
            }
            if (StrokeColor != GraphvizColor.Black)
            {
                properties["color"] = StrokeColor;
            }
            if (FillColor != GraphvizColor.White)
            {
                properties["fillcolor"] = FillColor;
            }
            if (Regular)
            {
                properties["regular"] = Regular;
            }
            if (Url != null)
            {
                properties["URL"] = Url;
            }
            if (ToolTip != null)
            {
                properties["tooltip"] = ToolTip;
            }
            if (Comment != null)
            {
                properties["comment"] = Comment;
            }
            if (Group != null)
            {
                properties["group"] = Group;
            }
            if (Layer != null)
            {
                properties["layer"] = Layer.Name;
            }
            if (Orientation > 0)
            {
                properties["orientation"] = Orientation;
            }
            if (Peripheries >= 0)
            {
                properties["peripheries"] = Peripheries;
            }
            if (Z > 0)
            {
                properties["z"] = Z;
            }
            if (Position != null)
            {
                properties["pos"] = $"{Position.X},{Position.Y}!";
            }
            if (Style == GraphvizVertexStyle.Diagonals
                || Shape == GraphvizVertexShape.MCircle
                || Shape == GraphvizVertexShape.MDiamond
                || Shape == GraphvizVertexShape.MSquare)
            {
                if (TopLabel != null)
                {
                    properties["toplabel"] = TopLabel;
                }
                if (BottomLabel != null)
                {
                    properties["bottomlabel"] = BottomLabel;
                }
            }
            if (Shape == GraphvizVertexShape.Polygon)
            {
                if (Sides != 0)
                {
                    properties["sides"] = Sides;
                }
                if (!IsZero(Skew))
                {
                    properties["skew"] = Skew;
                }
                if (!IsZero(Distortion))
                {
                    properties["distorsion"] = Distortion;
                }
            }

            return GenerateDot(properties);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToDot();
        }
    }
}