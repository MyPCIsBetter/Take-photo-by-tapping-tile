using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TapTileToTakePhoto
{
    /// <summary>
    /// Page that will take photo
    /// Code based on this tutorial: https://msdn.microsoft.com/en-us/windows/uwp/audio-video-camera/capture-photos-and-video-with-mediacapture
    /// </summary>
    public sealed partial class PhotoThanApp : Page
    {
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _externalCamera;
        private bool _mirroringPreview;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;

        public PhotoThanApp()
        {
            this.InitializeComponent();

            Application.Current.Resuming += Application_Resuming;
            Application.Current.Suspending += Application_Suspending;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            if (_mediaCapture != null)
            {
                CleanupCamera();
            }

            await InitializeCameraAsync();

            try
            {
                //e.Parameter - name of tile
                if (true || (e.Parameter != null && !String.IsNullOrEmpty(e.Parameter.ToString())))
                {
                    await TakePhotoAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(PhotoThanApp))
            {
                await InitializeCameraAsync();
            }
        }

        private void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(PhotoThanApp))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                CleanupCamera();

                deferral.Complete();
            }
        }

        private async Task InitializeCameraAsync()
        {

            if (_mediaCapture == null)
            {

                // Get available devices for capturing pictures
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                // Get the desired camera by panel
                DeviceInformation cameraDevice =
                    allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null &&
                    x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);

                // If there is no camera on the specified panel, get any camera
                cameraDevice = cameraDevice ?? allVideoDevices.FirstOrDefault();

                if (cameraDevice == null)
                {
                    //ShowMessageToUser("No camera device found.");
                    Debug.WriteLine("No camera device found.");
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when video recording has reached the maximum time and when something goes wrong
                //_mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

                var mediaInitSettings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(mediaInitSettings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    //ShowMessageToUser("The app was denied access to the camera");
                    Debug.WriteLine("The app was denied access to the camera");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                // If initialization succeeded, start the preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                    
                    //Why CaptureElement is here: http://stackoverflow.com/a/34190537/1804027
                    var captureElement = new CaptureElement();
                    captureElement.Source = _mediaCapture;

                    //Why StartPreviewAsync is here: http://stackoverflow.com/q/15390861/1804027 (dark photos)
                    await _mediaCapture.StartPreviewAsync();

                    //UpdateCaptureControls();
                }
            }
        }

        private async Task TakePhotoAsync()
        {
            var stream = new InMemoryRandomAccessStream();

            try
            {
                Debug.WriteLine("Taking photo...");
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                Debug.WriteLine("Photo taken!");

                var photoOrientation = ConvertOrientationToPhotoOrientation(GetCameraOrientation());
                await ReencodeAndSavePhotoAsync(stream, "photo.jpg", photoOrientation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when taking a photo: {0}", ex.ToString());
            }

        }

        private SimpleOrientation GetCameraOrientation()
        {
            if (_externalCamera)
            {
                // Cameras that are not attached to the device do not rotate along with it, so apply no rotation
                return SimpleOrientation.NotRotated;
            }

            // If the preview is being mirrored for a front-facing camera, then the rotation should be inverted
            if (_mirroringPreview)
            {
                // This only affects the 90 and 270 degree cases, because rotating 0 and 180 degrees is the same clockwise and counter-clockwise
                switch (_deviceOrientation)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return _deviceOrientation;
        }

        private static PhotoOrientation ConvertOrientationToPhotoOrientation(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return PhotoOrientation.Rotate90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return PhotoOrientation.Rotate180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return PhotoOrientation.Rotate270;
                case SimpleOrientation.NotRotated:
                default:
                    return PhotoOrientation.Normal;
            }
        }

        private static async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, string filename, PhotoOrientation photoOrientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);

                var file = await KnownFolders.PicturesLibrary.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);

                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(photoOrientation, PropertyType.UInt16) } };

                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }
        }

        /// <summary>
        /// Cleans up the camera resources (after stopping any video recording and/or preview if necessary) and unregisters from MediaCapture events
        /// </summary>
        /// <returns></returns>
        private void CleanupCamera()
        {
            Debug.WriteLine("CleanupCameraAsync");

            if (_isInitialized)
            {
                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }
    }
}
