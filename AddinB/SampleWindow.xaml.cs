using System.Reflection;
using System.Windows;
using SampleXamlLibrary;

namespace AddinB;

public partial class SampleWindow : Window
{
  public SampleWindow()
  {
    InitializeComponent();
    ContentHost.Content = new ViewModel();
  }

  public object CurrentContent => ContentHost.Content;

  public Assembly CurrentContentAssembly => CurrentContent.GetType().Assembly;

  public Assembly? TemplateDataTypeAssembly
  {
    get
    {
      foreach (var key in Resources.Keys)
      {
        if (key is DataTemplateKey { DataType: Type dataType })
        {
          return dataType.Assembly;
        }
      }

      return null;
    }
  }

  public bool HasTemplateForCurrentContent()
  {
    return TryFindResource(new DataTemplateKey(CurrentContent.GetType())) is DataTemplate;
  }
}
