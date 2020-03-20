﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using JPEGexplorer.Core.Models;
using JPEGexplorer.Models;
using JPEGexplorer.Services;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace JPEGexplorer.Views
{
    public sealed partial class MasterDetailDetailControl : UserControl
    {
        public ImageItem SourceImage
        {
            get { return GetValue(SourceImageProperty) as ImageItem; }
            set { SetValue(SourceImageProperty, value); }
        }

        public SampleOrder MasterMenuItem
        {
            get { return GetValue(MasterMenuItemProperty) as SampleOrder; }
            set { SetValue(MasterMenuItemProperty, value); }
        }

        public static readonly DependencyProperty SourceImageProperty = DependencyProperty.Register("SourceImage",
            typeof(SampleImage),
            typeof(MasterDetailDetailControl),
            new PropertyMetadata(null, OnSourceImagePropertyChanged));
        public static readonly DependencyProperty MasterMenuItemProperty = DependencyProperty.Register("MasterMenuItem",
            typeof(SampleOrder),
            typeof(MasterDetailDetailControl),
            new PropertyMetadata(null, OnMasterMenuItemPropertyChanged));

        public JPEGByteFile SourceByteFile { get; set; }

        public MasterDetailDetailControl()
        {
            InitializeComponent();
        }

        // Delete all except the save button
        private void ClearBlockChildren()
        {
            object o = block.Children[0];
            block.Children.Clear();
            block.Children.Add(o as UIElement);
        }

        public async Task HandleSelectedImage(ImageItem i)
        {
            SourceImage = i;

            JPEGByteFile file = new JPEGByteFile();

            try
            {
                file = await JPEGAnalyzerService.GetFileSegmentsAsync(i.File);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Excepion thrown while reading file segments: {e}");
            }

            UpdateDisplayedMetadataSegments(file);
        }

        public void UpdateDisplayedMetadataSegments(JPEGByteFile file)
        {
            SourceByteFile = file;

            ClearBlockChildren();

            int segmentCntr = 0;
            foreach (Segment s in SourceByteFile.Segments)
            {
                TextBlock title = new TextBlock
                {
                    Text = s.Name,
                    Style = (Style)Application.Current.Resources["DetailSubTitleStyle"],
                    Margin = (Thickness)Application.Current.Resources["SmallTopMargin"]
                };
                block.Children.Add(title);

                TextBlock content = new TextBlock
                {
                    Text = "Length: " + s.Length.ToString() + " B, Excess bytes after segment: " + s.ExcessBytesAfterSegment.ToString() + " B",
                    Style = (Style)Application.Current.Resources["DetailBodyBaseMediumStyle"]
                };
                block.Children.Add(content);

                if (s.Removable)
                {
                    Button removeSegmentButton = new Button()
                    {
                        Content = "Remove segment",
                        Tag = "BTN-" + segmentCntr
                    };

                    removeSegmentButton.Click += RemoveSegmentButton_Click;
                    block.Children.Add(removeSegmentButton);
                }

                segmentCntr++;
            }
        }

        private void RemoveSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((sender as Button).Tag as string).Split('-').Last());

            SourceByteFile.RemoveSegment(index);
            UpdateDisplayedMetadataSegments(SourceByteFile);
            //await SourceByteFile.SaveFile();
        }

        private static void OnSourceImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MasterDetailDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }

        private static void OnMasterMenuItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MasterDetailDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
            
        }

        private async void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourceByteFile != null && SourceByteFile.Modified)
            {
                await SourceByteFile.SaveFile();
            }
        }

        private void UndoLastChangeButton_Click(object sender, RoutedEventArgs e)
        {
            SourceByteFile?.UndoLastSegmentRemoval();
            UpdateDisplayedMetadataSegments(SourceByteFile);
        }
    }
}
