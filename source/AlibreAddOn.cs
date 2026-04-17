using AlibreAddOn;
using AlibreX;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using IStream = System.Runtime.InteropServices.ComTypes.IStream;
using MessageBox = System.Windows.MessageBox;
namespace AlibreAddOnAssembly
{
    public static class AlibreAddOn
    {
        private static IADRoot AlibreRoot { get; set; }
        private static AddOnRibbon TheAddOnInterface { get; set; }
        public static void AddOnLoad(IntPtr hwnd, IAutomationHook pAutomationHook, IntPtr unused)
        {
            AlibreRoot = (IADRoot)pAutomationHook.Root;
            TheAddOnInterface = new AddOnRibbon(AlibreRoot);
        }
        public static void AddOnUnload(IntPtr hwnd, bool forceUnload, ref bool cancel, int reserved1, int reserved2)
        {
            TheAddOnInterface = null;
            AlibreRoot = null;
        }
        public static void AddOnInvoke(IntPtr pAutomationHook, string sessionName, bool isLicensed, int reserved1, int reserved2) { }
        public static IAlibreAddOn GetAddOnInterface() => TheAddOnInterface;
        public static IADRoot GetRoot() => AlibreRoot;
    }
    public class AddOnRibbon : IAlibreAddOn
    {
        private readonly MenuManager _menuManager;
        private readonly IADRoot _alibreRoot;
        public AddOnRibbon(IADRoot alibreRoot)
        {
            _alibreRoot = alibreRoot;
            _menuManager = new MenuManager();
        }
        public int RootMenuItem => _menuManager.GetRootMenuItem().Id;
        public bool HasSubMenus(int menuID) => _menuManager.GetMenuItemById(menuID)?.SubItems.Count > 0;
        public Array SubMenuItems(int menuID) => _menuManager.GetMenuItemById(menuID)?.SubItems.Select(subItem => subItem.Id).ToArray();
        public string MenuItemText(int menuID) => _menuManager.GetMenuItemById(menuID)?.Text;
        public string MenuItemToolTip(int menuID) => _menuManager.GetMenuItemById(menuID)?.ToolTip;
        public string MenuIcon(int menuID) => null;
        public IAlibreAddOnCommand InvokeCommand(int menuID, string sessionIdentifier)
        {
            var session = _alibreRoot.Sessions.Item(sessionIdentifier);
            var menuItem = _menuManager.GetMenuItemById(menuID);
            return menuItem?.Command?.Invoke(session);
        }
        public ADDONMenuStates MenuItemState(int menuID, string sessionIdentifier) => ADDONMenuStates.ADDON_MENU_ENABLED;
        public bool PopupMenu(int menuID) => false;
        public bool HasPersistentDataToSave(string sessionIdentifier) => false;
        public void SaveData(IStream pCustomData, string sessionIdentifier) { }
        public void LoadData(IStream pCustomData, string sessionIdentifier) { }
        public bool UseDedicatedRibbonTab() => false;
        void IAlibreAddOn.setIsAddOnLicensed(bool isLicensed) { }
        public void LoadData(global::AlibreAddOn.IStream pCustomData, string sessionIdentifier)
        {
            throw new NotImplementedException();
        }
        public void SaveData(global::AlibreAddOn.IStream pCustomData, string sessionIdentifier)
        {
            throw new NotImplementedException();
        }
    }
    public class MenuItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public string Icon { get; set; }
        public Func<IADSession, IAlibreAddOnCommand> Command { get; set; }
        public List<MenuItem> SubItems { get; set; } = new List<MenuItem>();
        public MenuItem(int id, string text, string toolTip = "No tooltip available", string icon = null)
        {
            Id = id;
            Text = text;
            ToolTip = toolTip;
            Icon = null; // Icon functionality is disabled
        }
        public void AddSubItem(MenuItem subItem) => SubItems.Add(subItem);
        public IAlibreAddOnCommand? RunCmd(IADSession session)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select STL File for Conversion",
                Filter = "STL Files (*.stl)|*.stl|All Files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return null;
            string stlPath = ofd.FileName;
            string stepPath = Path.ChangeExtension(stlPath, ".stp");
            string addInDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string exePath = Path.Combine(addInDir, "stltostp.exe");
            if (!File.Exists(exePath))
            {
                MessageBox.Show($"Converter executable not found at the expected location:\n{exePath}", "Executable Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            string args = $"\"{stlPath}\" \"{stepPath}\"";
            try
            {
                var psi = new ProcessStartInfo(exePath, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    MessageBox.Show("Failed to start the external conversion process.", "Process Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                string stdOutput = proc.StandardOutput.ReadToEnd();
                string stdError = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0 && File.Exists(stepPath))
                {
                    FileInfo stepFileInfo = new FileInfo(stepPath);
                    string successMessage = $"Successfully created STEP file:\n{stepPath}\n\nSize: {stepFileInfo.Length / 1024.0:F2} KB";
                    MessageBox.Show(successMessage, "Conversion Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    try
                    {
                        IADRoot alibreRoot = AlibreAddOn.GetRoot();
                        if (alibreRoot != null)
                        {
                            alibreRoot.ImportSTEPFile(stepPath);
                        }
                        else
                        {
                            MessageBox.Show("Could not get the Alibre Design root object to import the file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while importing the STEP file '{Path.GetFileName(stepPath)}':\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    string errorDetails = $"Failed to convert '{Path.GetFileName(stlPath)}'.\n";
                    errorDetails += $"\nProcess exited with code: {proc.ExitCode}.";
                    if (!string.IsNullOrWhiteSpace(stdOutput))
                        errorDetails += $"\n\nStandard Output:\n{stdOutput}";
                    if (!string.IsNullOrWhiteSpace(stdError))
                        errorDetails += $"\n\nStandard Error:\n{stdError}";
                    if (!File.Exists(stepPath))
                        errorDetails += $"\n\nOutput file was not created: {stepPath}";
                    MessageBox.Show(errorDetails, "Conversion Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred during the conversion process:\n{ex.Message}", "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
    }
    public class MenuManager
    {
        private readonly MenuItem _rootMenuItem;
        private readonly Dictionary<int, MenuItem> _menuItems;
        public MenuManager()
        {
            _rootMenuItem = new MenuItem(401, "STL Importer", "Convert STL file to a STEP file and import it.", "logo.ico");
            _menuItems = new Dictionary<int, MenuItem>();
            BuildMenus();
        }
        private void BuildMenus()
        {
            var runStlStp = new MenuItem(9089, "Import STL File", "Convert STL file to a STEP file and import it.", "logo.ico");
            runStlStp.Command = runStlStp.RunCmd;
            _rootMenuItem.AddSubItem(runStlStp);
            RegisterMenuItem(_rootMenuItem);
        }
        public MenuItem? GetMenuItemById(int id)
        {
            _menuItems.TryGetValue(id, out MenuItem? menuItem);
            return menuItem;
        }
        public MenuItem GetRootMenuItem() => _rootMenuItem;
        private void RegisterMenuItem(MenuItem menuItem)
        {
            _menuItems[menuItem.Id] = menuItem;
            foreach (var subItem in menuItem.SubItems)
                RegisterMenuItem(subItem);
        }
    }
}