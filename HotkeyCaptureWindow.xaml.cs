using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Soundpad;

public partial class HotkeyCaptureWindow : Window
{
    public Key ResultKey { get; private set; }
    public ModifierKeys ResultModifiers { get; private set; }
    public bool IsSuccess { get; private set; }

    public HotkeyCaptureWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true; // Блокируем стандартную обработку

        Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

        // Игнорируем нажатия только модификаторов, ждем основную клавишу
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        if (key == Key.Escape)
        {
            IsSuccess = false;
        }
        else
        {
            ResultKey = key;
            ResultModifiers = Keyboard.Modifiers;
            IsSuccess = true;
        }

        this.DialogResult = true; // Закрывает окно
    }
}