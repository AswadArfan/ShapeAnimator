using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ShapeAnimator
{
    public partial class MainWindow : Window
    {
        String ActiveTool = "Circle";
        SolidColorBrush ActiveColor = Brushes.White;
        private double x;
        private double y;
        private bool firstClick = true;
        private DispatcherTimer animationTimer;
        private DispatcherTimer bulletTimer;
        private double bulletSpeed = 10;
        private int score = 0;
        private bool isPlaying = false;
        private bool bulletInMotion = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupColorPalette();
            InitializeAnimationTimer();
            InitializeBulletPosition();
            ScoreLabel.Content = "Score: " + score;
        }

        private void SetupColorPalette()
        {
            string[] colors = { "Red", "Blue", "Green", "Yellow", "White", "Orange" };
            foreach (string colorName in colors)
            {
                Button colorButton = new Button();
                colorButton.Content = "";
                colorButton.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(colorName);
                colorButton.Width = 30;
                colorButton.Height = 30;
                colorButton.Margin = new Thickness(2);
                colorButton.Click += ColorButton_Click;
                ColorPalette.Children.Add(colorButton);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            ActiveColor = (SolidColorBrush)clickedButton.Background;
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            ActiveTool = "Circle";
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            ActiveTool = "Rectangle";
        }

        private void ShapeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (firstClick)
            {
                x = e.GetPosition(ShapeCanvas).X;
                y = e.GetPosition(ShapeCanvas).Y;
            }
            else
            {
                double x1 = e.GetPosition(ShapeCanvas).X;
                double y1 = e.GetPosition(ShapeCanvas).Y;

                if (ActiveTool == "Circle")
                {
                    double radius = Math.Sqrt(Math.Pow(x1 - x, 2) + Math.Pow(y1 - y, 2));

                    Ellipse ellipse = new Ellipse();
                    ellipse.Stroke = ActiveColor;
                    ellipse.Fill = Brushes.Transparent;
                    ellipse.StrokeThickness = 2;
                    ellipse.Width = radius * 2;
                    ellipse.Height = radius * 2;
                    ellipse.SetValue(Canvas.LeftProperty, x - radius);
                    ellipse.SetValue(Canvas.TopProperty, y - radius);

                    // Assign initial velocity
                    ellipse.Tag = new ShapeProperties { DX = 5, DY = 5 };

                    ShapeCanvas.Children.Add(ellipse);
                }
                else if (ActiveTool == "Rectangle")
                {
                    Rectangle rectangle = new Rectangle();
                    rectangle.Stroke = ActiveColor;
                    rectangle.Fill = Brushes.Transparent;
                    rectangle.StrokeThickness = 2;
                    rectangle.Width = Math.Abs(x1 - x);
                    rectangle.Height = Math.Abs(y1 - y);
                    rectangle.SetValue(Canvas.LeftProperty, Math.Min(x, x1));
                    rectangle.SetValue(Canvas.TopProperty, Math.Min(y, y1));

                    // Assign initial velocity
                    rectangle.Tag = new ShapeProperties { DX = 10, DY = 10 };

                    ShapeCanvas.Children.Add(rectangle);
                }
            }
            firstClick = !firstClick;
        }

        private void ShapeCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            firstClick = true;
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            MoveShapes();
        }

        private bool AreShapesColliding(Shape shape1, Shape shape2)
        {
            Rect rect1 = new Rect(Canvas.GetLeft(shape1), Canvas.GetTop(shape1), shape1.Width, shape1.Height);
            Rect rect2 = new Rect(Canvas.GetLeft(shape2), Canvas.GetTop(shape2), shape2.Width, shape2.Height);
            return rect1.IntersectsWith(rect2);
        }

        private void MoveShapes()
        {
            foreach (var child1 in ShapeCanvas.Children)
            {
                if (child1 is Shape shape1 && shape1.Tag is ShapeProperties properties1)
                {
                    double left1 = (double)shape1.GetValue(Canvas.LeftProperty);
                    double top1 = (double)shape1.GetValue(Canvas.TopProperty);

                    for (int i = 0; i < ShapeCanvas.Children.Count; i++)
                    {
                        if (ShapeCanvas.Children[i] is Shape shape2 && shape2 != shape1 && shape2.Tag is ShapeProperties properties2)
                        {
                            double left2 = (double)shape2.GetValue(Canvas.LeftProperty);
                            double top2 = (double)shape2.GetValue(Canvas.TopProperty);

                            if (AreShapesColliding(shape1, shape2))
                            {
                                double dx = left2 - left1;
                                double dy = top2 - top1;
                                double angle = Math.Atan2(dy, dx);
                                double overlapX = Math.Abs(Math.Cos(angle) * (shape1.Width / 2 + shape2.Width / 2)) - Math.Abs(dx);
                                double overlapY = Math.Abs(Math.Sin(angle) * (shape1.Height / 2 + shape2.Height / 2)) - Math.Abs(dy);

                                if (Math.Abs(overlapX) < Math.Abs(overlapY))
                                {
                                    // Separate along X-axis
                                    shape1.SetValue(Canvas.LeftProperty, left1 - overlapX * Math.Sign(dx) / 2);
                                    shape2.SetValue(Canvas.LeftProperty, left2 + overlapX * Math.Sign(dx) / 2);
                                    properties1.DX = -properties1.DX;
                                    properties2.DX = -properties2.DX;
                                }
                                else
                                {
                                    // Separate along Y-axis
                                    shape1.SetValue(Canvas.TopProperty, top1 - overlapY * Math.Sign(dy) / 2);
                                    shape2.SetValue(Canvas.TopProperty, top2 + overlapY * Math.Sign(dy) / 2);
                                    properties1.DY = -properties1.DY;
                                    properties2.DY = -properties2.DY;
                                }
                            }
                        }
                    }

                    // Update shape position
                    double newLeft = left1 + properties1.DX;
                    double newTop = top1 + properties1.DY;

                    // Check boundaries and reverse direction if needed
                    if (newLeft < 0 || newLeft + shape1.Width > ShapeCanvas.ActualWidth)
                    {
                        properties1.DX = -properties1.DX;
                    }
                    if (newTop < 0 || newTop + shape1.Height > ShapeCanvas.ActualHeight)
                    {
                        properties1.DY = -properties1.DY;
                    }

                    // Move the shape
                    shape1.SetValue(Canvas.LeftProperty, left1 + properties1.DX);
                    shape1.SetValue(Canvas.TopProperty, top1 + properties1.DY);
                }
            }
        }
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            // Remove all shapes from the canvas
            ShapeCanvas.Children.OfType<Shape>().ToList().ForEach(shape => ShapeCanvas.Children.Remove(shape));

            // Reset the score
            score = 0;
            ScoreLabel.Content = "Score: " + score;

            // Optionally, reset the position of the gun and bullet
            // CenterGunAndBullet();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = true;
            animationTimer.Start();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = false;
            animationTimer.Stop();
        }

        private void InitializeAnimationTimer()
        {
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(30);
            animationTimer.Tick += (s, e) =>
            {
                if (isPlaying)
                {
                    MoveShapes();
                }
            };
        }

        private void InitializeGunPosition()
        {
            // Set the initial position of the gun to the center of the canvas
            double gunLeft = (ShapeCanvas.ActualWidth - ShuttleImage.Width) / 2;
            Canvas.SetLeft(ShuttleImage, gunLeft);
        }

        private void InitializeBulletPosition()
        {
            // Set the initial position of the bullet to the center of the canvas
            double bulletLeft = (ShapeCanvas.ActualWidth - BulletImage.Width) / 2;
            double bulletTop = (ShapeCanvas.ActualHeight - BulletImage.Height) / 2;
            Canvas.SetLeft(BulletImage, bulletLeft);
            Canvas.SetTop(BulletImage, bulletTop);
        }

        private void CenterGunAndBullet()
        {
            double canvasWidth = ShapeCanvas.ActualWidth;
            double canvasHeight = ShapeCanvas.ActualHeight;

            double gunWidth = ShuttleImage.Width;
            double gunHeight = ShuttleImage.Height;
            double bulletWidth = BulletImage.Width;
            double bulletHeight = BulletImage.Height;

            // Calculate the position of the gun in the center horizontally and at the bottom vertically
            double gunX = (canvasWidth - gunWidth) / 2;
            double gunY = canvasHeight - gunHeight;

            // Calculate the position of the bullet a little to the left of the gun
            double bulletX = gunX + gunWidth - 75; // Adjusted by 45 pixels to the left
            double bulletY = gunY + (gunHeight - bulletHeight) / 2;

            // Set the positions of the gun and bullet
            Canvas.SetLeft(ShuttleImage, gunX);
            Canvas.SetTop(ShuttleImage, gunY);

            Canvas.SetLeft(BulletImage, bulletX);
            Canvas.SetTop(BulletImage, bulletY);
        }




        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double canvasWidth = ShapeCanvas.ActualWidth;
            double gunPosition = Canvas.GetLeft(ShuttleImage);
            double bulletPosition = Canvas.GetLeft(BulletImage);
            double movementStep = 10;

            if (e.Key == Key.Left)
            {
                // Move the gun and bullet 10px to the left
                double newGunPosition = Math.Max(0, gunPosition - movementStep); // Ensure within canvas boundaries
                double newBulletPosition = Math.Max(0, bulletPosition - movementStep); // Ensure within canvas boundaries

                // Update gun and bullet positions
                Canvas.SetLeft(ShuttleImage, newGunPosition);
                Canvas.SetLeft(BulletImage, newBulletPosition);
            }
            else if (e.Key == Key.Right)
            {
                // Move the gun and bullet 10px to the right
                double newGunPosition = Math.Min(canvasWidth - ShuttleImage.Width, gunPosition + movementStep); // Ensure within canvas boundaries
                double newBulletPosition = Math.Min(canvasWidth - BulletImage.Width, bulletPosition + movementStep); // Ensure within canvas boundaries

                // Update gun and bullet positions
                Canvas.SetLeft(ShuttleImage, newGunPosition);
                Canvas.SetLeft(BulletImage, newBulletPosition);
            }
            else if (e.Key == Key.Space)
            {
                Shoot_Click(sender, e);
            }
        }


        private void Shoot_Click(object sender, RoutedEventArgs e)
        {
            // Determine bullet's starting position relative to ShuttleImage (gun)
            double gunX = Canvas.GetLeft(ShuttleImage);
            double gunY = Canvas.GetTop(ShuttleImage);

            // Adjust these offsets to position the bullet relative to the gun
            double bulletOffsetX = 0; // Adjust as needed
            double bulletOffsetY = 0; // Adjust as needed

            // Set bullet's initial position
            Canvas.SetLeft(BulletImage, gunX + bulletOffsetX);
            Canvas.SetTop(BulletImage, gunY - BulletImage.Height - bulletOffsetY);

            // Start the bullet movement timer
            bulletTimer = new DispatcherTimer();
            bulletTimer.Interval = TimeSpan.FromMilliseconds(30);
            bulletTimer.Tick += (s, args) => MoveBullet(bulletTimer); // Pass the timer instance
            bulletTimer.Start();
        }




        private void MoveBullet(DispatcherTimer bulletTimer)
        {
            double bulletSpeed = 10; // Adjust as needed
            double bulletX = Canvas.GetLeft(BulletImage);
            double bulletY = Canvas.GetTop(BulletImage);
            bool bulletHit = false;

            // Update bullet position to move upwards
            bulletY -= bulletSpeed;
            Canvas.SetTop(BulletImage, bulletY);

            // Check collision with shapes
            foreach (var shape in ShapeCanvas.Children.OfType<Shape>())
            {
                // Get the bounding box of the shape
                Rect shapeBounds = new Rect(Canvas.GetLeft(shape), Canvas.GetTop(shape), shape.Width, shape.Height);

                // Check if the bullet collides with the shape
                if (shapeBounds.Contains(bulletX + BulletImage.Width / 2, bulletY))
                {
                    // Remove the shape from the canvas
                    ShapeCanvas.Children.Remove(shape);
                    bulletHit = true;

                    // Increase the score
                    score++;

                    // Update the score label
                    ScoreLabel.Content = "Score: " + score;

                    break; // Exit the loop since bullet hit a shape
                }
            }

            // If the bullet goes out of the canvas or hits a shape, stop the timer and reset bullet position
            if (bulletHit || bulletY + BulletImage.Height < 0)
            {
                // Stop this bullet's movement timer
                bulletTimer.Stop();

                // Reset bullet position for next shoot
                CenterBulletPosition();
            }
        }
        private void CenterBulletPosition()
        {
            double gunX = Canvas.GetLeft(ShuttleImage);
            double gunY = Canvas.GetTop(ShuttleImage);
            double bulletOffsetX = 15; // Adjust as needed
            double bulletOffsetY = 10; // Adjust as needed

            Canvas.SetLeft(BulletImage, gunX + bulletOffsetX);
            Canvas.SetTop(BulletImage, gunY - BulletImage.Height - bulletOffsetY);
        }






        private void ResetBullet()
        {
            double gunLeft = Canvas.GetLeft(ShuttleImage);
            double gunTop = Canvas.GetTop(ShuttleImage);
            double bulletLeft = gunLeft + (ShuttleImage.Width - BulletImage.Width) / 2;
            double bulletTop = gunTop - BulletImage.Height;
            Canvas.SetLeft(BulletImage, bulletLeft);
            Canvas.SetTop(BulletImage, bulletTop);
        }

        private void InitializeBulletTimer()
        {
            bulletTimer = new DispatcherTimer();
            bulletTimer.Interval = TimeSpan.FromMilliseconds(30);
            bulletTimer.Tick += (s, e) =>
            {
                if (bulletInMotion)
                {
                    double bulletTop = Canvas.GetTop(BulletImage);
                    bulletTop -= bulletSpeed;
                    Canvas.SetTop(BulletImage, bulletTop);

                    if (bulletTop <= 0)
                    {
                        bulletInMotion = false;
                        bulletTimer.Stop();
                        ResetBullet();
                    }

                    CheckBulletCollision();
                }
            };
        }

        private void CheckBulletCollision()
        {
            Rect bulletRect = new Rect(Canvas.GetLeft(BulletImage), Canvas.GetTop(BulletImage), BulletImage.Width, BulletImage.Height);

            foreach (UIElement element in ShapeCanvas.Children)
            {
                if (element is Shape shape)
                {
                    Rect shapeRect = new Rect(Canvas.GetLeft(shape), Canvas.GetTop(shape), shape.Width, shape.Height);

                    if (bulletRect.IntersectsWith(shapeRect))
                    {
                        ShapeCanvas.Children.Remove(shape);
                        bulletInMotion = false;
                        bulletTimer.Stop();
                        ResetBullet();
                        score += 10;
                        ScoreLabel.Content = "Score: " + score;
                        break;
                    }
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterGunAndBullet();
        }
    }

    public class ShapeProperties
    {
        public double DX { get; set; }
        public double DY { get; set; }
    }
}
