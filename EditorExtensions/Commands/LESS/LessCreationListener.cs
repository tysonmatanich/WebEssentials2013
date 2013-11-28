﻿using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(HtmlContentTypeDefinition.HtmlContentType)]
    [ContentType(LessContentTypeDefinition.LessContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class LessViewCreationListener : IWpfTextViewConnectionListener
    {
        [Import, System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!subjectBuffers.Any(b => b.ContentType.IsOfType(LessContentTypeDefinition.LessContentType)))
                return;

            var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);

            textView.Properties.GetOrCreateSingletonProperty<LessExtractVariableCommandTarget>(() => new LessExtractVariableCommandTarget(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty<LessExtractMixinCommandTarget>(() => new LessExtractMixinCommandTarget(textViewAdapter, textView));
        }
        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }
    }
}
