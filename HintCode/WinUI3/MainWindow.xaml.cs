using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace WinUI3
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<ListItem> ListItems { get; } = new ObservableCollection<ListItem>();

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "WinUI 3 Horizontal List Demo";

            // Add sample items
            ListItems.Add(new ListItem
            {
                Title = "Mountain View",
                Description = "A beautiful mountain landscape with snow peaks and clear blue sky.",
                ImagePath = "ms-appx:///Assets/Mountain.jpg",
                IconGlyph = "\uE774"
            }); // Mountain icon

            ListItems.Add(new ListItem
            {
                Title = "Beach Paradise",
                Description = "Tropical beach with palm trees and crystal clear water.",
                ImagePath = "ms-appx:///Assets/Beach.jpg",
                IconGlyph = "\uE706"
            }); // Beach icon

            ListItems.Add(new ListItem
            {
                Title = "Urban Landscape",
                Description = "Modern cityscape with skyscrapers and busy streets.",
                ImagePath = "ms-appx:///Assets/City.jpg",
                IconGlyph = "\uEC02"
            }); // City icon

            ListItems.Add(new ListItem
            {
                Title = "Forest Retreat",
                Description = "Dense forest with tall trees and a path leading through.",
                ImagePath = "ms-appx:///Assets/Forest.jpg",
                IconGlyph = "\uEA86"
            }); // Tree icon

            ListItems.Add(new ListItem
            {
                Title = "Desert Adventure",
                Description = "Vast desert landscape with sand dunes stretching to the horizon.",
                ImagePath = "ms-appx:///Assets/Desert.jpg",
                IconGlyph = "\uE753"
            }); // Sun icon

            ListItems.Add(new ListItem
            {
                Title = "Island Getaway",
                Description = "Secluded island with lush vegetation surrounded by blue ocean.",
                ImagePath = "ms-appx:///Assets/Island.jpg",
                IconGlyph = "\uE909"
            }); // Island icon

            // Set the items source
            HorizontalListView.ItemsSource = ListItems;
        }

        private void HorizontalListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HorizontalListView.SelectedItem is ListItem selectedItem)
            {
                // You can handle item selection here
                // For example, show details in another pane or navigate to a details page

                // Reset selection (optional)
                HorizontalListView.SelectedItem = null;
            }
        }
    }

    public class ListItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public string IconGlyph { get; set; }
    }
}