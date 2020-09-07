using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shutters
{
    /// <summary>
    /// Interaction logic for RollerShutter.xaml
    /// </summary>
    public partial class RollerShutter : UserControl
    {
        public RollerShutter()
        {
            InitializeComponent();

            createAnimations();
        }

        private RollerShutterViewModel Shutter { get => DataContext as RollerShutterViewModel; }

        private void RollerShutter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Shutter == null)
            {
                return;
            }
            UpdateAnimation(ShutterStatus.Unknown, Shutter.Status);
            Shutter.PropertyChanging += Shutter_PropertyChanging;
            Shutter.PropertyChanged += Shutter_PropertyChanged;
        }

        private ShutterStatus _changingStatus;
        private void Shutter_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (Shutter != null && e.PropertyName == nameof(RollerShutterViewModel.Status))
                {
                    _changingStatus = Shutter.Status;
                }
            });
        }

        private void Shutter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (Shutter != null)
                {
                    if (e.PropertyName == nameof(RollerShutterViewModel.Status))
                    {
                        var changedStatus = Shutter.Status;
                        UpdateAnimation(_changingStatus, changedStatus);
                    }
                    if (e.PropertyName == nameof(RollerShutterViewModel.IsDisabled))
                    {
                        bool isDisabled = Shutter.IsDisabled;
                        if (isDisabled)
                        {
                            stopStoryBoards();
                        }
                    }
                }
            });
        }

        private void stopStoryBoards()
        {
            if (isOpenStoryBoardRunning)
            {
                openStoryboard.Stop(this);
                openStoryboard.Remove(this);
                isOpenStoryBoardRunning = false;
            }
            if (isCloseStoryBoardRunning)
            {
                closeStoryboard.Stop(this);
                closeStoryboard.Remove(this);
                isCloseStoryBoardRunning = false;
            }
        }

        private void UpdateAnimation(ShutterStatus oldStatus, ShutterStatus newStatus)
        {
            Logger.LogVerbose($"{Shutter.Name} updating display: oldStatus {oldStatus}, newStatus {newStatus}");
            if (oldStatus == newStatus)
            {
                return;
            }
            stopStoryBoards();
            switch (newStatus)
            {
                case ShutterStatus.Open:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 00);
                    break;
                case ShutterStatus.Closed:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 320);
                    break;
                case ShutterStatus.Half:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 200);
                    break;
                case ShutterStatus.Opening:
                    openStoryboard.Begin(this, true);
                    isOpenStoryBoardRunning = true;
                    break;
                case ShutterStatus.Closing:
                    closeStoryboard.Begin(this, true);
                    isCloseStoryBoardRunning = true;
                    break;
                case ShutterStatus.Unknown:
                default:
                    shutterImage.Opacity = 0.3;
                    break;
            }
        }

        Storyboard openStoryboard;
        RectAnimationUsingKeyFrames openAnimation;
        bool isOpenStoryBoardRunning = false;

        Storyboard closeStoryboard;
        RectAnimationUsingKeyFrames closeAnimation;
        bool isCloseStoryBoardRunning = false;

        RectangleGeometry shutterClipRectangleGeometry;

        private void createAnimations()
        { 
            // Create a RectangleGeometry to sanimate the clipping rectangle.
            shutterClipRectangleGeometry = new RectangleGeometry();
            shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 320);
            shutterImage.Clip = shutterClipRectangleGeometry;

            this.RegisterName(
                "AnimatedRectangleGeometry", shutterClipRectangleGeometry);

            openAnimation = new RectAnimationUsingKeyFrames();
            openAnimation.Duration = TimeSpan.FromSeconds(2);
            openAnimation.RepeatBehavior = RepeatBehavior.Forever;

            // LinearRectKeyFrame creates a smooth, linear animation between values.
            openAnimation.KeyFrames.Add(
                new LinearRectKeyFrame(
                    new Rect(0, 120, 500, 320), // Target value (KeyValue)
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))) // KeyTime
                );
            openAnimation.KeyFrames.Add(
                new LinearRectKeyFrame(
                    new Rect(0, 120, 500, 0), // Target value (KeyValue)
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))) // KeyTime
                );

            // Set the animation to target the Rect property
            // of the object named "AnimatedRectangleGeometry."
            Storyboard.SetTargetName(openAnimation, "AnimatedRectangleGeometry");
            Storyboard.SetTargetProperty(openAnimation, new PropertyPath(RectangleGeometry.RectProperty));

            // create a closeAnimation the same way
            closeAnimation = new RectAnimationUsingKeyFrames();
            closeAnimation.Duration = TimeSpan.FromSeconds(2);

            // Set the animation to repeat forever. 
            closeAnimation.RepeatBehavior = RepeatBehavior.Forever;

            closeAnimation.KeyFrames.Add(
                new LinearRectKeyFrame(
                    new Rect(0, 120, 500, 0), // Target value (KeyValue)
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))) // KeyTime
                );

            closeAnimation.KeyFrames.Add(
                new LinearRectKeyFrame(
                    new Rect(0, 120, 500, 320), // Target value (KeyValue)
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))) // KeyTime
                );

            // Set the animation to target the Rect property
            // of the object named "AnimatedRectangleGeometry."
            Storyboard.SetTargetName(closeAnimation, "AnimatedRectangleGeometry");
            Storyboard.SetTargetProperty(closeAnimation, new PropertyPath(RectangleGeometry.RectProperty));

            // Create the storyboards to apply the animation.
            openStoryboard = new Storyboard();
            openStoryboard.Children.Add(openAnimation);
            closeStoryboard = new Storyboard();
            closeStoryboard.Children.Add(closeAnimation);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debugger.Log(1, "", $"{Shutter?.Name} clicked \r\n");
            if (Shutter == null)
            {
                return;
            }
            switch(Shutter.Status)
            {
                case ShutterStatus.Opening:
                case ShutterStatus.Closing:
                    await Shutter.Stop();
                    break;
                case ShutterStatus.Open:
                    await Shutter.Close();
                    break;
                case ShutterStatus.Closed:
                    await Shutter.Open();
                    break;
                case ShutterStatus.Unknown:
                default:
                    MessageBox.Show($"Status of shutter '{Shutter.Name}' is '{Shutter.Status}', not clear what to do upon a simple click. Use right-click menu to select a command.");
                    return;
            }
        }

        private async void Up_Click(object sender, RoutedEventArgs e)
        {
            if (Shutter == null)
            {
                return;
            }
            switch (Shutter.Status)
            {
                case ShutterStatus.Closed:
                case ShutterStatus.Half:
                    await Shutter.Open();
                    break;
                default:
                    return;
            }
        }

        private async void Down_Click(object sender, RoutedEventArgs e)
        {
            if (Shutter == null)
            {
                return;
            }
            switch (Shutter.Status)
            {
                case ShutterStatus.Open:
                case ShutterStatus.Half:
                    await Shutter.Close();
                    break;
                default:
                    return;
            }
        }

        private async void Half_Click(object sender, RoutedEventArgs e)
        {
            if (Shutter == null)
            {
                return;
            }
            switch (Shutter.Status)
            {
                case ShutterStatus.Open:
                    await Shutter.Half();
                    break;
                case ShutterStatus.Closed:
                    await Shutter.Half();
                    break;
                default:
                    return;
            }
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Shutter == null)
            {
                return;
            }
            switch (Shutter.Status)
            {
                case ShutterStatus.Opening:
                case ShutterStatus.Closing:
                    await Shutter.Stop();
                    break;
                default:
                    return;
            }
        }

        private void Button_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement fe = e.Source as FrameworkElement;
            ContextMenu theMenu = new ContextMenu();
            if (Shutter == null)
            {
                return;
            }
            switch (Shutter.Status)
            {
                case ShutterStatus.Opening:
                case ShutterStatus.Closing:
                    {
                        var item = new MenuItem()
                        {
                            Header = "Stop"
                        };
                        item.Click += Stop_Click;
                        theMenu.Items.Add(item);
                    }
                    break;
                case ShutterStatus.Open:
                    {
                        var downItem = new MenuItem()
                        {
                            Header = "Down"
                        };
                        downItem.Click += Down_Click;
                        theMenu.Items.Add(downItem);
                        var halfItem = new MenuItem()
                        {
                            Header = "Half"
                        };
                        halfItem.Click += Half_Click;
                        theMenu.Items.Add(halfItem);
                    }
                    break;
                case ShutterStatus.Closed:
                    {
                        var upItem = new MenuItem()
                        {
                            Header = "Up"
                        };
                        upItem.Click += Up_Click;
                        theMenu.Items.Add(upItem);
                        var halfItem = new MenuItem()
                        {
                            Header = "Half"
                        };
                        halfItem.Click += Half_Click;
                        theMenu.Items.Add(halfItem);
                    }
                    break;
                case ShutterStatus.Half:
                    {
                        var upItem = new MenuItem()
                        {
                            Header = "Up"
                        };
                        upItem.Click += Up_Click;
                        theMenu.Items.Add(upItem);
                        var downItem = new MenuItem()
                        {
                            Header = "Down"
                        };
                        downItem.Click += Down_Click;
                        theMenu.Items.Add(downItem);
                    }
                    break;
                case ShutterStatus.Unknown:
                default:
                    return;
            }
            fe.ContextMenu = theMenu;
        }
    }
}
