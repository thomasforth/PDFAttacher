using java.io;
using java.util;
using Microsoft.Win32;
using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.pdmodel.common.filespecification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PDFAttacher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // some example code here https://svn.apache.org/repos/asf/pdfbox/trunk/examples/src/main/java/org/apache/pdfbox/examples/pdmodel/ExtractEmbeddedFiles.java

        // Abe's tool also has some good example code

        // adapting the Java at -- https://stackoverflow.com/questions/36157415/list-pdf-attachments-using-pdfbox-java

        Dictionary<String, PDComplexFileSpecification> embeddedFileNamesNet;
        PDDocument pd;
        private void LoadPDFAndLookForAttachments(string PDFPath)
        {
            try
            {
                pd.close();
            }
            catch
            {
                // pd isn't open
            }

            FileDropStatus.Text = "";

            if (!String.Equals(System.IO.Path.GetExtension(PDFPath),".pdf",StringComparison.CurrentCultureIgnoreCase))
            {
                MessageBoxResult result = MessageBox.Show("PDF Attacher only reads PDFs.","PDF expected.");
            }
            else
            {
                FileDropStatus.Text = System.IO.Path.GetFileName(PDFPath);
                pd = PDDocument.load(PDFPath);

                // Get attachments and save out as a file
                PDDocumentCatalog catalog = pd.getDocumentCatalog();
                PDDocumentNameDictionary names = catalog.getNames();
                PDEmbeddedFilesNameTreeNode embeddedFiles = names.getEmbeddedFiles();

                Map embeddedFileNames = embeddedFiles.getNames();
                embeddedFileNamesNet = embeddedFileNames.ToDictionary<String, PDComplexFileSpecification>();

                AttachmentsPanel.Children.Clear();
                //For-Each Loop is used to list all embedded files (if there is more than one)          
                foreach (KeyValuePair<String, PDComplexFileSpecification> entry in embeddedFileNamesNet)
                {
                    StackPanel attachmentPanel = new StackPanel();
                    attachmentPanel.Orientation = Orientation.Vertical;
                    attachmentPanel.Margin = new Thickness(5);
                    attachmentPanel.MinWidth = 90;
                    System.Windows.Controls.Image attachmentImage = new System.Windows.Controls.Image();
                    attachmentImage.Height = 32;
                    attachmentImage.Width = 32;

                    attachmentPanel.Tag = entry.Key;

                    attachmentPanel.MouseEnter += AttachmentPanel_MouseEnter;
                    attachmentPanel.MouseLeave += AttachmentPanel_MouseLeave;
                    attachmentPanel.MouseUp += AttachmentPanel_MouseUp;

                    Icon attachmentIcon = ShellIcon.GetLargeIconFromExtension(System.IO.Path.GetExtension(entry.Key));
                    BitmapSource attachmentBitmapSource = Imaging.CreateBitmapSourceFromHIcon(attachmentIcon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    attachmentImage.Source = attachmentBitmapSource;
                    attachmentImage.Margin = new Thickness(5, 5, 5, 0);
                    attachmentIcon.Dispose();

                    TextBlock attachmentTextBlock = new TextBlock();
                    attachmentTextBlock.Margin = new Thickness(5);
                    attachmentTextBlock.Text = System.IO.Path.GetFileName(entry.Key);

                    System.Windows.Controls.Image deleteImage = new System.Windows.Controls.Image();
                    deleteImage.Height = 20;
                    deleteImage.Width = 20;
                    deleteImage.Margin = new Thickness(0, 6, -50, -6);

                    // var uriSource = new Uri(@"/PDFBox_sharp;component/DeleteIcon.png", UriKind.Relative);
                    // deleteImage.Source = new BitmapImage(uriSource);

                    // attachmentPanel.Children.Add(deleteImage);
                    attachmentPanel.Children.Add(attachmentImage);
                    attachmentPanel.Children.Add(attachmentTextBlock);

                    AttachmentsPanel.Children.Add(attachmentPanel);
                }

                Icon sysicon = System.Drawing.Icon.ExtractAssociatedIcon(PDFPath);
                BitmapSource bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                PDFIcon.Source = bmpSrc;
                sysicon.Dispose();
            }
            //pd.close();            
        }

        private string selectedFile;
        private void AttachmentPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            deselectAllAttachmentPanels();

            StackPanel _stackPanel = (StackPanel)sender;
            selectedFile = (string)_stackPanel.Tag;
            _stackPanel.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 215, 215));
            ButtonsStackPanel.Visibility = Visibility.Visible;

            e.Handled = true;
        }

        private void AttachmentPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            StackPanel _stackPanel = (StackPanel)sender;
            if (ButtonsStackPanel.Visibility != Visibility.Visible)
            {
                _stackPanel.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
        }

        private void AttachmentPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            StackPanel _stackPanel = (StackPanel)sender;
            if (ButtonsStackPanel.Visibility != Visibility.Visible)
            {
                _stackPanel.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 255));
            }
        }

        private string pathToCurrentPDF;
        private void LoadPDF_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new OpenFileDialog();
            openPicker.InitialDirectory = Environment.CurrentDirectory;
            openPicker.Filter = "pdf file(*.pdf) | *.pdf";
            openPicker.ShowDialog();

            // Application now has read/write access to the picked file             

            // StorageFile sFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(filename));
            // IList<string> lines = await FileIO.ReadLinesAsync(ppfile); // haha -- reading the whole file 4GB will break everything

            pathToCurrentPDF = openPicker.FileName;
            LoadPDFAndLookForAttachments(pathToCurrentPDF);
        }

        private void FileDropZone_Drop(object sender, DragEventArgs e)
        {
            // reset UI
            FileDropZoneBorder.BorderThickness = new Thickness(1);
            FileDropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));

            // look for PDF attachments
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 1)
                {
                    MessageBoxResult result = MessageBox.Show("Only one PDF at a time.");
                }

                if (files.Length == 1)
                {
                    pathToCurrentPDF = files[0];
                    LoadPDFAndLookForAttachments(pathToCurrentPDF);
                }
                
            }
        }

        // Decent example here -- https://github.com/Aiybe/PDFData/blob/master/od-reader/src/main/java/im/abe/pdfdata/AttachmentDataStorage.java

        private void AttachmentZone_Drop(object sender, DragEventArgs e)
        {
            // reset UI
            AttachmentZoneBorder.BorderThickness = new Thickness(1);
            AttachmentZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            deselectAllAttachmentPanels();

            // attach files to PDF
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> filesList = files.ToList();

                if (pd != null) {
                    foreach (string file in filesList)
                    {
                        byte[] fileBytes = System.IO.File.ReadAllBytes(file);

                        FileStream _filestream = System.IO.File.Open(file, FileMode.Open);
                        string nameOfFileToAttach = _filestream.Name;
                        _filestream.Close();

                        PDComplexFileSpecification fs = new PDComplexFileSpecification();
                        fs.setFile(nameOfFileToAttach);

                        ByteArrayInputStream inputStream = new ByteArrayInputStream(fileBytes, 0, fileBytes.Length);
                        //ByteArrayInputStream inputStream = new ByteArrayInputStream(fileBytes);
                        PDEmbeddedFile embeddedFile = new PDEmbeddedFile(pd, inputStream);
                        embeddedFile.setModDate(java.util.Calendar.getInstance());
                        embeddedFile.setSize(fileBytes.Length);
                        fs.setEmbeddedFile(embeddedFile);

                        PDDocumentCatalog catalog = pd.getDocumentCatalog();
                        PDDocumentNameDictionary names = catalog.getNames();
                        PDEmbeddedFilesNameTreeNode embeddedFiles = names.getEmbeddedFiles();
                        Map embeddedFileNames = embeddedFiles.getNames();
                        Dictionary<String, PDComplexFileSpecification> embeddedFileNamesNet = embeddedFileNames.ToDictionary<String, PDComplexFileSpecification>();

                        Map TomsNewMap = new HashMap();

                        // Attach all the existing files         
                        foreach (KeyValuePair<String, PDComplexFileSpecification> entry in embeddedFileNamesNet)
                        {
                            TomsNewMap.put(entry.Key, entry.Value);
                        }
                        
                        // Attach the new file
                        TomsNewMap.put(System.IO.Path.GetFileName(nameOfFileToAttach), fs);
                        PDEmbeddedFilesNameTreeNode TomsEmbeddedFiles = new PDEmbeddedFilesNameTreeNode();
                        TomsEmbeddedFiles.setNames(TomsNewMap);
                        names.setEmbeddedFiles(TomsEmbeddedFiles);
                        catalog.setNames(names);

                        pd.save(pathToCurrentPDF);
                        pd.close();
                    }
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show("No PDF file loaded.");
                }
            }

            // reload the PDF
            LoadPDFAndLookForAttachments(pathToCurrentPDF);
        }

        private void AttachmentZone_DragEnter(object sender, DragEventArgs e)
        {
            AttachmentZoneBorder.BorderThickness = new Thickness(4);
            AttachmentZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
        }

        private void AttachmentZone_DragLeave(object sender, DragEventArgs e)
        {
            AttachmentZoneBorder.BorderThickness = new Thickness(1);
            AttachmentZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        }

        private void FileDropZone_DragEnter(object sender, DragEventArgs e)
        {
            FileDropZoneBorder.BorderThickness = new Thickness(4);
            FileDropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
        }

        private void FileDropZone_DragLeave(object sender, DragEventArgs e)
        {
            FileDropZoneBorder.BorderThickness = new Thickness(1);
            FileDropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "All(*.*)|*",
                InitialDirectory = System.IO.Path.GetDirectoryName(pathToCurrentPDF),
                FileName = selectedFile
            };

            if (dialog.ShowDialog() == true)
            {
                PDComplexFileSpecification _file;
                embeddedFileNamesNet.TryGetValue(selectedFile, out _file);

                PDEmbeddedFile ef = _file.getEmbeddedFile();
                byte[] fileByteArray = ef.getByteArray();

                System.IO.File.WriteAllBytes(dialog.FileName, fileByteArray);
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            // to delete we just mark for not to be reincluded and do a round of saving
            PDDocumentCatalog catalog = pd.getDocumentCatalog();
            PDDocumentNameDictionary names = catalog.getNames();
            PDEmbeddedFilesNameTreeNode embeddedFiles = names.getEmbeddedFiles();
            Map embeddedFileNames = embeddedFiles.getNames();
            Dictionary<String, PDComplexFileSpecification> embeddedFileNamesNet = embeddedFileNames.ToDictionary<String, PDComplexFileSpecification>();

            Map TomsNewMap = new HashMap();

            // Attach all the existing files         
            foreach (KeyValuePair<String, PDComplexFileSpecification> entry in embeddedFileNamesNet)
            {
                if (selectedFile == entry.Key)
                {
                    //
                }
                else
                {
                    TomsNewMap.put(entry.Key, entry.Value);
                }
            }

            PDEmbeddedFilesNameTreeNode TomsEmbeddedFiles = new PDEmbeddedFilesNameTreeNode();
            TomsEmbeddedFiles.setNames(TomsNewMap);
            names.setEmbeddedFiles(TomsEmbeddedFiles);
            catalog.setNames(names);

            pd.save(pathToCurrentPDF);

            // reload the PDF
            LoadPDFAndLookForAttachments(pathToCurrentPDF);
            ButtonsStackPanel.Visibility = Visibility.Hidden;
        }

        private void AttachmentZone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonsStackPanel.Visibility = Visibility.Hidden;
            deselectAllAttachmentPanels();
        }

        private void deselectAllAttachmentPanels()
        {
            UIElementCollection stackpanels = AttachmentsPanel.Children;
            foreach (UIElement stackpanel in stackpanels)
            {
                ((StackPanel)stackpanel).Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }
        }
    }
    // as per https://stackoverflow.com/questions/17935473/best-way-to-convert-a-java-map-to-a-dictionary-in-c-sharp
    public static class JavaUtils
    {
        public static Dictionary<K, V> ToDictionary<K, V>(this java.util.Map map)
        {
            var dict = new Dictionary<K, V>();
            var iterator = map.keySet().iterator();
            while (iterator.hasNext())
            {
                var key = (K)iterator.next();
                dict.Add(key, (V)map.get(key));
            }
            return dict;
        }
    }


    //via https://gist.github.com/madd0/1433330

    // -----------------------------------------------------------------------
    // <copyright file="ShellIcon.cs" company="Mauricio DIAZ ORLICH (madd0@madd0.com)">
    //   Distributed under Microsoft Public License (MS-PL).
    //   http://www.opensource.org/licenses/MS-PL
    // </copyright>
    // -----------------------------------------------------------------------

    /// <summary>
    /// Get a small or large Icon with an easy C# function call
    /// that returns a 32x32 or 16x16 System.Drawing.Icon depending on which function you call
    /// either GetSmallIcon(string fileName) or GetLargeIcon(string fileName)
    /// </summary>
    public static class ShellIcon
    {
        #region Interop constants

        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

        #endregion

        #region Interop data types

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [Flags]
        private enum SHGFI : int
        {
            /// <summary>get icon</summary>
            Icon = 0x000000100,
            /// <summary>get display name</summary>
            DisplayName = 0x000000200,
            /// <summary>get type name</summary>
            TypeName = 0x000000400,
            /// <summary>get attributes</summary>
            Attributes = 0x000000800,
            /// <summary>get icon location</summary>
            IconLocation = 0x000001000,
            /// <summary>return exe type</summary>
            ExeType = 0x000002000,
            /// <summary>get system icon index</summary>
            SysIconIndex = 0x000004000,
            /// <summary>put a link overlay on icon</summary>
            LinkOverlay = 0x000008000,
            /// <summary>show icon in selected state</summary>
            Selected = 0x000010000,
            /// <summary>get only specified attributes</summary>
            Attr_Specified = 0x000020000,
            /// <summary>get large icon</summary>
            LargeIcon = 0x000000000,
            /// <summary>get small icon</summary>
            SmallIcon = 0x000000001,
            /// <summary>get open icon</summary>
            OpenIcon = 0x000000002,
            /// <summary>get shell size icon</summary>
            ShellIconSize = 0x000000004,
            /// <summary>pszPath is a pidl</summary>
            PIDL = 0x000000008,
            /// <summary>use passed dwFileAttribute</summary>
            UseFileAttributes = 0x000000010,
            /// <summary>apply the appropriate overlays</summary>
            AddOverlays = 0x000000020,
            /// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
            OverlayIndex = 0x000000040,
        }

        #endregion

        private class Win32
        {
            [DllImport("shell32.dll")]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            [DllImport("User32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);

        }

        public static Icon GetSmallFolderIcon()
        {
            return GetIcon("folder", SHGFI.SmallIcon | SHGFI.UseFileAttributes, true);
        }

        public static Icon GetLargeFolderIcon()
        {
            return GetIcon("folder", SHGFI.LargeIcon | SHGFI.UseFileAttributes, true);
        }

        public static Icon GetSmallIcon(string fileName)
        {
            return GetIcon(fileName, SHGFI.SmallIcon);
        }

        public static Icon GetLargeIcon(string fileName)
        {
            return GetIcon(fileName, SHGFI.LargeIcon);
        }

        public static Icon GetSmallIconFromExtension(string extension)
        {
            return GetIcon(extension, SHGFI.SmallIcon | SHGFI.UseFileAttributes);
        }

        public static Icon GetLargeIconFromExtension(string extension)
        {
            return GetIcon(extension, SHGFI.LargeIcon | SHGFI.UseFileAttributes);
        }

        private static Icon GetIcon(string fileName, SHGFI flags, bool isFolder = false)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            IntPtr hImgSmall = Win32.SHGetFileInfo(fileName, isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.Icon | flags));

            Icon icon = (Icon)System.Drawing.Icon.FromHandle(shinfo.hIcon).Clone();
            Win32.DestroyIcon(shinfo.hIcon);
            return icon;
        }
    }




}
