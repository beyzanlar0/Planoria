namespace Planoria;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        bool loggedIn = Preferences.Get("CurrentUserId", 0) != 0;
        Page start = loggedIn ? new AppShell() : new NavigationPage(new RegisterPage());
        return new Window(start);
    }
}