using Avalonia;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace AvaloniaEdit.Tests.Rendering
{
    internal class TextViewTests
    {
        // https://github.com/AvaloniaUI/Avalonia/blob/master/src/Headless/Avalonia.Headless/HeadlessPlatformStubs.cs#L126
        private const int HeadlessGlyphAdvance = 8;
        
        [AvaloniaTest]
        public void Visual_Line_Should_Create_Two_Text_Lines_When_Wrapping()
        {
            TextView textView = new TextView();

            TextDocument document = new TextDocument("hello world".ToCharArray());   

            textView.Document = document;

            ((ILogicalScrollable)textView).CanHorizontallyScroll = false;
            textView.Width = HeadlessGlyphAdvance * 8;

            textView.Measure(Size.Infinity);

            VisualLine visualLine = textView.GetOrConstructVisualLine(document.Lines[0]);

            Assert.AreEqual(2, visualLine.TextLines.Count);
            Assert.AreEqual("hello ", new string(visualLine.TextLines[0].TextRuns[0].Text.Span));
            Assert.AreEqual("world", new string(visualLine.TextLines[1].TextRuns[0].Text.Span));
        }

        [AvaloniaTest]
        public void Visual_Line_Should_Create_One_Text_Lines_When_Not_Wrapping()
        {
            TextView textView = new TextView();

            TextDocument document = new TextDocument("hello world".ToCharArray());

            textView.Document = document;
            textView.EnsureVisualLines();
            ((ILogicalScrollable)textView).CanHorizontallyScroll = false;
            textView.Width = HeadlessGlyphAdvance * 500;

            textView.Measure(Size.Infinity);

            VisualLine visualLine = textView.GetOrConstructVisualLine(document.Lines[0]);

            Assert.AreEqual(1, visualLine.TextLines.Count);
            Assert.AreEqual("hello world", new string(visualLine.TextLines[0].TextRuns[0].Text.Span));
        }

        // AvaloniaEdit#401: with FlowDirection RightToLeft the line must be laid out RTL so the
        // TextFormatter's bidi pass reorders runs. Mixed Latin+Hebrew "AB<alef><bet>" is stored in
        // logical order; visually, an LTR base keeps Latin first, an RTL base puts the Hebrew first.
        // Before the fix the paragraph FlowDirection was hardcoded LeftToRight, so both directions
        // produced the same (LTR) order.
        [AvaloniaTest]
        public void Rtl_FlowDirection_Reorders_Bidi_Runs()
        {
            const string mixed = "ABאב"; // AB + Hebrew alef, bet

            string FirstVisualRun(FlowDirection flowDirection)
            {
                var textView = new TextView { FlowDirection = flowDirection };
                textView.Document = new TextDocument(mixed.ToCharArray());
                ((ILogicalScrollable)textView).CanHorizontallyScroll = true;
                textView.Measure(Size.Infinity);
                var visualLine = textView.GetOrConstructVisualLine(textView.Document.Lines[0]);
                return new string(visualLine.TextLines[0].TextRuns[0].Text.Span);
            }

            var ltrFirst = FirstVisualRun(FlowDirection.LeftToRight);
            var rtlFirst = FirstVisualRun(FlowDirection.RightToLeft);

            // LTR keeps the Latin run first; RTL reorders so the Hebrew run comes first.
            Assert.AreEqual("AB", ltrFirst);
            Assert.AreNotEqual(ltrFirst, rtlFirst);
            Assert.IsTrue(rtlFirst.StartsWith("א"), $"expected RTL first run to start with Hebrew, got '{rtlFirst}'");
        }
    }
}
