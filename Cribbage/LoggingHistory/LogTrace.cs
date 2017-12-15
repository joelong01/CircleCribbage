using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Cribbage
{

    public class LogTrace
    {
        private string _filename = "eCribbage.log.crib";
        private string DIRECTORY_NAME = "LogFiles";
        private StorageFile _logFile = null;
        private bool _debugTrace = true;
        private int _count = 0;
        IRandomAccessStream _stream;

        public LogTrace()
        {


        }


        public bool EnableLogging
        {
            get
            {
                return _debugTrace;
            }
            set
            {
                _debugTrace = value;
                if (!_debugTrace)
                    Close();
            }

        }

        public string FQP
        {

            get
            {
                if (_logFile != null)
                    return _logFile.Path;

                return "Unitialized";
            }

        }

        public async Task Init(bool debugTrace)
        {

            
            _debugTrace = debugTrace;
            _filename = String.Format("{0}\\eCribbage Log_{1}.crib", DIRECTORY_NAME, DateTime.Now.Ticks.ToString());
            _logFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(_filename, CreationCollisionOption.ReplaceExisting);
            Debug.WriteLine(_logFile.Path);
            _stream = await _logFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);


        }

        public void Close()
        {
            if (_stream != null)
            {
                _logFile = null;
                _stream.Dispose();
            }
            _count = 0;
            _debugTrace = false;

        }

        public static async Task WriteFile(string filename, string contents)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync("DataCache", Windows.Storage.CreationCollisionOption.OpenIfExists);
            var file = await folder.CreateFileAsync(filename, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            var fs = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
            fs.Seek(fs.Size);
            var outStream = fs.GetOutputStreamAt(0);
            var dataWriter = new Windows.Storage.Streams.DataWriter(outStream);
            dataWriter.WriteString(contents);
            await dataWriter.StoreAsync();
            dataWriter.DetachStream();
            await outStream.FlushAsync();
        }

        public async Task<string> ReadLogFile(string fileName)
        {
            if (fileName != this.FQP)
            {

                int index = fileName.IndexOf(DIRECTORY_NAME);
                fileName = fileName.Substring(index);
                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                if (file == null)
                    return "";

                return await Windows.Storage.FileIO.ReadTextAsync(file);
            }

            if (_logFile != null)
            {
                using (var inputStream = _stream.GetInputStreamAt(0))
                {
                    Windows.Storage.Streams.DataReader reader = new Windows.Storage.Streams.DataReader(inputStream);
                    await reader.LoadAsync((uint)_stream.Size);
                    string data = reader.ReadString((uint)_stream.Size);
                    reader.DetachStream();
                    return data;


                }

            }

            return "";            

        }

        public async Task<bool> DeleteFile(string fileName)
        {
            if (fileName != this.FQP)
            {

                int index = fileName.IndexOf(DIRECTORY_NAME);
                fileName = fileName.Substring(index);
                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                await file.DeleteAsync();
                return true;
            }

            else
            {
                MessageDialog dlg = new MessageDialog("Cannot delete the log for the current game.  Turn off logging and then delete the file.", "eCribbage");
                await dlg.ShowAsync();
                return false;

            }

        }


        private async Task WriteLogEntry(string logEntry)
        {
            if (_stream == null)
                await Init(true);

            if (_logFile != null)
            {                
                using (var outputStream = _stream.GetOutputStreamAt(_stream.Size))
                {
                                     
                    DataWriter dataWriter = new DataWriter(outputStream);
                    dataWriter.WriteString(logEntry);
                    await dataWriter.StoreAsync();
                    await outputStream.FlushAsync();
                }
            }

        }

        public async Task WriteLogEntryTask (string s)
        {
            if (!_debugTrace)
                return;
            
            s = String.Format("{0}\t{1}\t{2}", _count++, DateTime.Now.TimeOfDay.ToString(), s);
            await WriteLogEntry(s);

        }

        public async Task GetLogFiles(ObservableCollection<string> list)
        {

            StorageFolder logFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(DIRECTORY_NAME, CreationCollisionOption.OpenIfExists);
            IReadOnlyList<IStorageItem> itemsList = await logFolder.GetItemsAsync();
            foreach (var item in itemsList)
            {
                list.Insert(0, item.Path);
            }
        }

        public enum TraceOptions { ForceOutput = 0x01, ForceFile = 0x10, ForceBoth = 0x11, Default };

        public async Task<string> TraceMessage(string message, TraceOptions options = TraceOptions.Default, 
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        {

            if (_debugTrace && options == TraceOptions.Default)
                options = TraceOptions.ForceBoth;

                      string s = String.Format("[{0}\t{1} [{2}, {3}.{4}]: {5}", _count++, DateTime.Now.TimeOfDay, memberName, GetFileNameFromFQPN(sourceFilePath), sourceLineNumber, message);

            if ((options & TraceOptions.ForceOutput) == TraceOptions.ForceOutput)
                Debug.WriteLine(s);

            if ((options & TraceOptions.ForceFile) == TraceOptions.ForceFile)               
            {
                await WriteLogEntryTask(s);
            }
            return s;
        }

        private string GetFileNameFromFQPN(string fqpn)
        {
            int pos = fqpn.LastIndexOf("\\");
            if (pos == 0) return fqpn;
            pos++;
            return fqpn.Substring(pos, fqpn.Length - pos);
        }

        public string TraceMessageAsync(string message, TraceOptions options = TraceOptions.Default,
       [CallerMemberName] string memberName = "",
       [CallerFilePath] string sourceFilePath = "",
       [CallerLineNumber] int sourceLineNumber = 0)
        {

            try
            {

                if (_debugTrace && options == TraceOptions.Default)
                    options = TraceOptions.ForceBoth;


                string s = String.Format("[{0}\t{1} [{2}, {3}.{4}]: {5}", _count++, DateTime.Now.TimeOfDay, memberName, GetFileNameFromFQPN(sourceFilePath), sourceLineNumber, message);

                if ((options & TraceOptions.ForceOutput) == TraceOptions.ForceOutput)
                    Debug.WriteLine(s);

                if ((options & TraceOptions.ForceFile) == TraceOptions.ForceFile)
                {
                    WriteLogEntryAsync(s);
                } 
                
                return s;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception writing TraceAsync: {0}\nMessage:\n{1}", e.ToString(), message);
                
            }

            return "Exception thrown";
        }

        /// <summary>
        ///      Async Void on purpose
        /// </summary>
        /// <param name="s"></param>
        private async void WriteLogEntryAsync(string s)
        {
            if (_stream == null || _logFile == null)
            {
                throw new InvalidOperationException("stream hasn't been initialized in the log - call sync method first");
            }
             using (var outputStream = _stream.GetOutputStreamAt(_stream.Size))
                {

                    DataWriter dataWriter = new DataWriter(outputStream);
                   dataWriter.WriteString(s);
                   await dataWriter.StoreAsync();
                   await outputStream.FlushAsync();
                }
            }
        }
       

    

}
