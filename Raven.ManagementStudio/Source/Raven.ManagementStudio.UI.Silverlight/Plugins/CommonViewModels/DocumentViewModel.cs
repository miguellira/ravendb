namespace Raven.ManagementStudio.UI.Silverlight.Plugins.CommonViewModels
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Text;
    using System.Windows;
    using System.Windows.Markup;
    using Caliburn.Micro;
    using Controls;
    using Messages;
    using Models;
    using Newtonsoft.Json.Linq;
    using Plugin;

    public class DocumentViewModel : Screen, IRavenScreen
    {
        private Document document;
        private bool isSelected;
        private string jsonData;

        public DocumentViewModel(Document document, IRavenScreen parent)
        {
            this.DisplayName = "Doc";
            this.Document = document;
            this.jsonData = PrepareRawJsonString(document.Data);
            this.Thumbnail = new DocumentThumbnail();
            this.ParentRavenScreen = parent;
            CompositionInitializer.SatisfyImports(this);


            this.CustomizedThumbnailTemplate = (FrameworkElement)XamlReader.Load(@"
                <Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                        Height='150' Width='110' BorderBrush='LightGray' BorderThickness='1'>
                    <TextBlock Text='{Binding JsonData}' TextWrapping='Wrap' />
                </Border>");
        }

        public DocumentThumbnail Thumbnail { get; set; }

        public FrameworkElement CustomizedThumbnailTemplate { get; set; }

        public IRavenScreen ParentRavenScreen { get; set; }

        public Document Document
        {
            get
            {
                return this.document;
            }

            set
            {
                NotifyOfPropertyChange(() => this.Document);
                this.document = value;
            }
        }

        [Import]
        public IEventAggregator EventAggregator { get; set; }

        public string JsonData
        {
            get
            {
                return this.jsonData;
            }

            set
            {
                this.jsonData = value;
                NotifyOfPropertyChange(() => this.JsonData);
            }
        }

        public bool IsSelected
        {
            get 
            { 
                return this.isSelected; 
            }

            set
            {
                this.isSelected = value;
                NotifyOfPropertyChange(() => this.IsSelected);
            }
        }

        public void ShowDocument()
        {
            this.EventAggregator.Publish(new ReplaceActiveScreen(this));
        }

        private static string PrepareRawJsonString(IEnumerable<KeyValuePair<string, JToken>> data)
        {
            var result = new StringBuilder("{\n");

            foreach (var item in data)
            {
                result.Append(item.Key).Append(" : ").Append(item.Value).Append("\n");
            }

            result.Append("}");

            return result.ToString();
        }
    }
}