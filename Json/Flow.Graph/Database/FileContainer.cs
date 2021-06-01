﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class FileDatabase : EntityDatabase
    {
        private readonly    string  databaseFolder;
        private readonly    bool    pretty;

        public FileDatabase(string databaseFolder, bool pretty = true) {
            this.pretty = pretty;
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new FileContainer(name, this, databaseFolder + name, pretty);
        }
    }
    
    public class FileContainer : EntityContainer
    {
        private readonly    string          folder;

        public  override    bool            Pretty      { get; }


        public FileContainer(string name, EntityDatabase database, string folder, bool pretty) : base (name, database) {
            this.Pretty = pretty;
            this.folder = folder + "/";
            Directory.CreateDirectory(folder);
        }

        private string FilePath(string key) {
            return folder + key + ".json";
        }
        
        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            Dictionary<string, EntityError> createErrors = null;
            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue payload = entityPair.Value;
                var path = FilePath(key);
                try {
                    await WriteText(path, payload.Json).ConfigureAwait(false);
                } catch (Exception e) {
                    var error = new EntityError(EntityErrorType.WriteError, name, key, e.Message);
                    AddEntityError(ref createErrors, key, error);
                }
            }
            return new CreateEntitiesResult{createErrors = createErrors};
        }

        public override async Task<UpdateEntitiesResult> UpdateEntities(UpdateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            Dictionary<string, EntityError> updateErrors = null;
            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue payload = entityPair.Value;
                var path = FilePath(key);
                try {
                    await WriteText(path, payload.Json).ConfigureAwait(false);
                } catch (Exception e) {
                    var error = new EntityError(EntityErrorType.WriteError, name, key, e.Message);
                    AddEntityError(ref updateErrors, key, error);
                }
            }
            return new UpdateEntitiesResult{updateErrors = updateErrors};
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var keys        = command.ids;
            var entities    = new Dictionary<string, EntityValue>(keys.Count);
            foreach (var key in keys) {
                var filePath = FilePath(key);
                EntityValue entry;
                if (File.Exists(filePath)) {
                    try {
                        var payload = await ReadText(filePath).ConfigureAwait(false);
                        entry = new EntityValue(payload);
                    } catch (Exception e) {
                        var error = new EntityError(EntityErrorType.ReadError, name, key, e.Message);
                        entry = new EntityValue(error);
                    }
                } else {
                    entry = new EntityValue();
                }
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesResult{entities = entities};
            result.ValidateEntities(name, syncContext);
            return result;
        }

        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            var ids     = GetIds(folder);
            var result  = await FilterEntities(command, ids, syncContext).ConfigureAwait(false);
            return result;
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            Dictionary<string, EntityError> deleteErrors = null;
            foreach (var key in keys) {
                string path = FilePath(key);
                try {
                    DeleteFile(path);
                } catch (Exception e) {
                    var error = new EntityError(EntityErrorType.DeleteError, name, key, e.Message);
                    AddEntityError(ref deleteErrors, key, error);
                }
            }
            var result = new DeleteEntitiesResult{deleteErrors = deleteErrors};
            return Task.FromResult(result);
        }
        
        
        // -------------------------------------- helper methods -------------------------------------- 
        private static HashSet<string> GetIds(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var ids = Helper.CreateHashSet<string>(fileNames.Length);
            for (int n = 0; n < fileNames.Length; n++) {
                var fileName = fileNames[n];
                var len = fileName.Length;
                var id = fileName.Substring(folder.Length, len - folder.Length - ".json".Length);
                ids.Add(id);
            }
            return ids;
        }
        
        private static async Task WriteText(string filePath, string text) {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);
            using (var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(encodedText, 0, encodedText.Length).ConfigureAwait(false);
            }
        }
        
        private static async Task<string> ReadText(string filePath) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var sb = new StringBuilder();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0) {
                    string text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }
                return sb.ToString();
            }
        }
        
        private static void DeleteFile(string filePath) {
            File.Delete(filePath);
        }
    }
}
