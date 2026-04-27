using System.Windows;
using System.Windows.Controls;

namespace SampleXamlLibrary;

public sealed class ViewModel
{
  public override string ToString()
  {
    return GetType().FullName ?? base.ToString()!;
  }
}

public sealed class UserControl : TextBlock
{
  public UserControl()
  {
    Margin = new Thickness(24);
    FontSize = 24;
    Text = "UserControl from SampleXamlLibrary";
  }
}
