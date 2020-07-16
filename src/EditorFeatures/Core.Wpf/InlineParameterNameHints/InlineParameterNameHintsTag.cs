﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// This is the tag which implements the IntraTextAdornmentTag and is meant to create the UIElements that get shown
    /// in the editor
    /// </summary>
    internal class InlineParameterNameHintsTag : IntraTextAdornmentTag
    {
        public const string TagId = "inline parameter name hints";

        /// <summary>
        /// Creates the UIElement on call
        /// Uses PositionAffinity.Successor because we want the tag to be associated with the following character
        /// </summary>
        /// <param name="text">The name of the parameter associated with the argument</param>
        public InlineParameterNameHintsTag(string text, double lineHeight, TextFormattingRunProperties format)
            : base(CreateElement(text, lineHeight, format), removalCallback: null, PositionAffinity.Successor)
        {
        }

        private static UIElement CreateElement(string text, double lineHeight, TextFormattingRunProperties format)
        {
            // Constructs the hint block which gets assigned parameter name and fontstyles according to the options
            // page. Calculates a font size 1/4 smaller than the font size of the rest of the editor
            var block = new TextBlock
            {
                Text = text + ":",
                VerticalAlignment = VerticalAlignment.Center,
                FontStyle = FontStyles.Normal,
                FontFamily = format.Typeface.FontFamily,
                FontSize = format.FontRenderingEmSize - (0.25 * format.FontHintingEmSize),
                Padding = new Thickness(0),
                Foreground = format.ForegroundBrush
            };

            // Encapsulates the textblock within a border. Sets the height of the border to be 3/4 of the original 
            // height. Gets foreground/background colors from the options menu. The margin is the distance from the 
            // adornment to the text
            var border = new Border
            {
                Child = block,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = format.BackgroundBrush,
                Padding = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                Height = lineHeight - (0.25 * lineHeight),
                CornerRadius = new CornerRadius(2),
            };

            border.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return border;
        }
    }
}
