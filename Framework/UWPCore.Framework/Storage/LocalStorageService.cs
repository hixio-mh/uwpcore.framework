﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace UWPCore.Framework.Storage
{
    /// <summary>
    /// The storage service of the local app storage.
    /// </summary>
    public sealed class LocalStorageService : IStorageService
    {
        #region Fields

        /// <summary>
        /// The local root folder.
        /// </summary>
        private StorageFolder _rootFolder = ApplicationData.Current.LocalFolder;

        #endregion

        #region Properties

        public IStorageFolder RootFolder
        {
            get
            {
                return _rootFolder;
            }
        }

        #endregion

        #region Public Methods

        public async Task WriteFile(string filePath, string data)
        {
            var folder = await GetStorageFolder(filePath);

            if (folder == null)
                return;

            var storageFile = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.OpenIfExists);
            await WriteFile(storageFile, data);
        }

        public async Task WriteFile(IStorageFile file, string data)
        {
            await FileIO.WriteTextAsync(file, data);
        }

        public async Task<bool> WriteFile(string filePath, Stream data)
        {
            var folder = await GetStorageFolder(filePath);

            if (folder == null)
                return false;

            var storageFile = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.OpenIfExists);
            return await WriteFile(storageFile, data);
        }

        public async Task<bool> WriteFile(IStorageFile file, Stream data)
        {
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream);
                using (var inputStream = data.AsInputStream())
                {
                    var dataReader = new DataReader(inputStream);
                    IBuffer buffer;

                    // store loaded data in isolated storage
                    var dataBuffer = new byte[1024];
                    while ((buffer = dataReader.ReadBuffer(1024)) != null)
                    {
                        dataWriter.WriteBuffer(buffer);

                        if (buffer.Length != 1024) // FIXME: how to detect the end of the stream?
                            break;
                    }
                }
            }

            return true;
        }

        public async Task<bool> WriteFile(string filePath, WriteableBitmap image)
        {
            var folder = await GetStorageFolder(filePath);

            if (folder == null)
                return false;

            var storageFile = await folder.CreateFileAsync(Path.GetFileName(filePath), CreationCollisionOption.ReplaceExisting);
            return await WriteFile(storageFile, image);
        }

        public async Task<bool> WriteFile(IStorageFile file, WriteableBitmap image)
        {
            var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                var dataWriter = new DataWriter(outputStream);
                dataWriter.WriteBuffer(image.PixelBuffer);
            }

            return true;
        }

        public async Task<string> ReadFile(string filePath)
        {
            // TODO: use StorageFile.GetFileFromPathAsync(); which can handle file path?
            var folder = await GetStorageFolder(filePath);

            if (folder == null)
                return null;

            try
            {
                var storageFile = await folder.GetFileAsync(Path.GetFileName(filePath));
                return await ReadFile(storageFile);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public async Task<string> ReadFile(IStorageFile file)
        {
            return await FileIO.ReadTextAsync(file);
        }

        public async Task<bool> ContainsFile(string filePath)
        {
            try
            {
                var folder = await GetStorageFolder(filePath);

                if (folder == null)
                    return false;

                await folder.GetFileAsync(Path.GetFileName(filePath));
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        public async Task<bool> ContainsDirectory(string directoryPath)
        {
            var folder = await GetStorageFolder(directoryPath, true);
            return folder != null;
        }

        public async Task<StorageFile> GetFileAsync(string filePath)
        {
            if (await ContainsFile(filePath))
            {
                return await RootFolder.GetFileAsync(filePath);
            }

            return null;
        }

        public async Task<IReadOnlyList<StorageFile>> GetFilesAsync(string filePath)
        {
            var folder = await GetFolderAsync(filePath);
            if (folder != null)
            {
                return await folder.GetFilesAsync();
            }

            return null;
        }

        public async Task<StorageFile> CreateOrGetFileAsync(string filePath)
        {
            try
            {
                return await RootFolder.CreateFileAsync(filePath, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<StorageFile> CreateOrReplaceFileAsync(string filePath)
        {
            try
            {
                return await RootFolder.CreateFileAsync(filePath, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                var folder = await GetStorageFolder(filePath);

                if (folder == null)
                    return;

                var file = await folder.GetFileAsync(Path.GetFileName(filePath));
                await file.DeleteAsync();
            }
            catch (FileNotFoundException)
            {
                // NOP
            }
        }

        public async Task<StorageFolder> GetFolderAsync(string path)
        {
            if (await ContainsDirectory(path))
            {
                return await RootFolder.GetFolderAsync(path);
            }

            return null;
        }

        public async Task<IReadOnlyList<StorageFolder>> GetFoldersAsync(string path)
        {
            var folder = await GetFolderAsync(path);
            if (folder != null)
            {
                return await folder.GetFoldersAsync();
            }

            return null;
        }

        public async Task<StorageFolder> CreateOrGetFolderAsync(string path)
        {
            return await RootFolder.CreateFolderAsync(path, CreationCollisionOption.OpenIfExists);
        }

        public async Task<StorageFolder> CreateOrReplaceFolderAsync(string path)
        {
            return await RootFolder.CreateFolderAsync(path, CreationCollisionOption.ReplaceExisting);
        }

        public async Task DeleteFolderAsync(string path)
        {
            try
            {
                var folder = await GetStorageFolder(path, true);

                if (folder == null)
                    return;

                await folder.DeleteAsync();
            }
            catch (FileNotFoundException)
            {
                // NOP
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the folder of the given file or directory path.
        /// </summary>
        /// <param name="fullPath">The file or directory path to get the folder.</param>
        /// <param name="isDirectoryPath">Indicates whether the given path
        /// is a directory path (false) or a file path (true, default).</param>
        /// <returns>Returns the found folder or NULL in case of an error.</returns>
        private async Task<IStorageFolder> GetStorageFolder(string fullPath, bool isDirectoryPath = false)
        {
            string directoryPath = isDirectoryPath ? fullPath : Path.GetDirectoryName(fullPath);
            string[] directoryNames = directoryPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            var currentFolder = RootFolder;
            foreach (var dirName in directoryNames)
            {
                try
                {
                    currentFolder = await currentFolder.GetFolderAsync(dirName);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }
            return currentFolder;
        }

        #endregion
    }

}
