using System;
using System.Collections.Generic;
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
        public static readonly DependencyProperty ShutterNameProperty = DependencyProperty.Register(
          nameof(ShutterName),
          typeof(string),
          typeof(RollerShutter)
        );

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
          nameof(Status),
          typeof(ShutterStatus),
          typeof(RollerShutter),
          new PropertyMetadata(OnStatusPropertyChanged)
        );

        public RollerShutter()
        {
            InitializeComponent();

            createAnimations();
        }

        public string ShutterName
        {
            get
            {
                return (string)GetValue(ShutterNameProperty);
            }
            set
            {
                SetValue(ShutterNameProperty, value);
            }
        }

        public ShutterStatus Status
        {
            get
            {
                return (ShutterStatus)GetValue(StatusProperty);
            }
            set
            {
                SetValue(StatusProperty, value);
            }
        }

        private static void OnStatusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as RollerShutter;
            var oldStatus = (ShutterStatus)e.OldValue;
            var newStatus = (ShutterStatus)e.NewValue;
            control.UpdateAnimation(oldStatus, newStatus);
        }

        private void UpdateAnimation(ShutterStatus oldStatus, ShutterStatus newStatus)
        {
            if (oldStatus == newStatus)
            {
                return;
            }
            switch (oldStatus)
            {
                case ShutterStatus.Opening:
                    openStoryboard.Stop(this);
                    openStoryboard.Remove(this);
                    break;
                case ShutterStatus.Closing:
                    closeStoryboard.Stop(this);
                    closeStoryboard.Remove(this);
                    break;
                case ShutterStatus.Unknown:
                    shutterImage.Opacity = 1;
                    break;
            }
            switch (newStatus)
            {
                case ShutterStatus.Open:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 00);
                    break;
                case ShutterStatus.Closed:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 320);
                    break;
                case ShutterStatus.Stopped:
                    shutterClipRectangleGeometry.Rect = new Rect(0, 120, 500, 200);
                    break;
                case ShutterStatus.Opening:
                    openStoryboard.Begin(this, true);
                    break;
                case ShutterStatus.Closing:
                    closeStoryboard.Begin(this, true);
                    break;
                case ShutterStatus.Unknown:
                default:
                    shutterImage.Opacity = 0.3;
                    break;
            }
        }

        Storyboard openStoryboard;
        RectAnimationUsingKeyFrames openAnimation;
        Storyboard closeStoryboard;
        RectAnimationUsingKeyFrames closeAnimation;
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
            System.Diagnostics.Debugger.Log(1, "", $"{ShutterName} clicked \r\n");
            var viewModel = DataContext as RollerShuttersViewModel;
            if (viewModel == null)
            {
                return;
            }
            switch(Status)
            {
                case ShutterStatus.Opening:
                case ShutterStatus.Closing:
                    await viewModel.Stop(ShutterName);
                    break;
                case ShutterStatus.Open:
                    await viewModel.Close(ShutterName);
                    break;
                case ShutterStatus.Closed:
                    await viewModel.Open(ShutterName);
                    break;
                case ShutterStatus.Unknown:
                default:
                    return;
            }
        }

        private async void Up_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as RollerShuttersViewModel;
            if (viewModel == null)
            {
                return;
            }
            switch (Status)
            {
                case ShutterStatus.Closed:
                case ShutterStatus.Stopped:
                    await viewModel.Open(ShutterName);
                    break;
                default:
                    return;
            }
        }

        private async void Down_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as RollerShuttersViewModel;
            if (viewModel == null)
            {
                return;
            }
            switch (Status)
            {
                case ShutterStatus.Open:
                case ShutterStatus.Stopped:
                    await viewModel.Close(ShutterName);
                    break;
                default:
                    return;
            }
        }

        private async void Half_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as RollerShuttersViewModel;
            if (viewModel == null)
            {
                return;
            }
            switch (Status)
            {
                case ShutterStatus.Open:
                    await viewModel.Half(ShutterName);
                    break;
                case ShutterStatus.Closed:
                    await viewModel.Half(ShutterName);
                    break;
                default:
                    return;
            }
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as RollerShuttersViewModel;
            if (viewModel == null)
            {
                return;
            }
            switch (Status)
            {
                case ShutterStatus.Opening:
                case ShutterStatus.Closing:
                    await viewModel.Stop(ShutterName);
                    break;
                default:
                    return;
            }
        }

        private void Button_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement fe = e.Source as FrameworkElement;
            ContextMenu theMenu = new ContextMenu();
            switch (Status)
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
                case ShutterStatus.Stopped:
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
