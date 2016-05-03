using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TapTileToTakePhoto
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void addAppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri messemgerTileImage = new Uri("ms-appx:///Assets/LockScreenLogo.scale-200.png");

                // During creation of secondary tile, an application may set additional arguments on the tile that will be passed in during activation.
                // These arguments should be meaningful to the application. In this sample, we'll pass in the date and time the secondary tile was pinned.
                //string tileActivationArguments = "SecondaryTile.Messenger" + " WasPinnedAt=" + DateTime.Now.ToLocalTime().ToString();
                string tileActivationArguments = "takephoto";

                SecondaryTile takePhotoTile = new SecondaryTile("SecondaryTile.TakePhoto", "TakePhoto", tileActivationArguments, messemgerTileImage, TileSize.Square150x150);

                bool isPinned = await takePhotoTile.RequestCreateForSelectionAsync(GetElementRect((FrameworkElement)sender), Windows.UI.Popups.Placement.Below);

                Debug.WriteLine("Pinned: " + isPinned);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Data);
            }
        }

        // Gets the rectangle of the element
        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //e.Parameter - tileActivationArguments kafelka
            if (!String.IsNullOrEmpty(e.Parameter.ToString()))
            {
                try
                {
                    //Nie można wywołać Navigate jeśli aktualny się nie zakończył
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                             () => Frame.Navigate(typeof(PhotoThanApp), e.Parameter));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}
