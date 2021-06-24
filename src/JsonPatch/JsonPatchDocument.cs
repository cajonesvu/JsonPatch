﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JsonPatch.Paths;

namespace JsonPatch
{
    public class JsonPatchDocument<TEntity> : IJsonPatchDocument where TEntity : class, new()
    {
        private List<JsonPatchOperation> _operations = new List<JsonPatchOperation>();

        public List<JsonPatchOperation> Operations { get { return _operations; } }

        public bool HasOperations { get { return _operations.Count > 0; } }

        public void Add(string path, object value)
        {
            _operations.Add(new JsonPatchOperation
            {
                Operation = JsonPatchOperationType.add,
                Path = path,
                ParsedPath = PathHelper.ParsePath(path, typeof(TEntity)),
                Value = value
            });
        }

        public void Replace(string path, object value)
        {
            _operations.Add(new JsonPatchOperation
            {
                Operation = JsonPatchOperationType.replace,
                Path = path,
                ParsedPath = PathHelper.ParsePath(path, typeof(TEntity)),
                Value = value
            });
        }

        public void Remove(string path)
        {
            _operations.Add(new JsonPatchOperation
            {
                Operation = JsonPatchOperationType.remove,
                Path = path,
                ParsedPath = PathHelper.ParsePath(path, typeof(TEntity))
            });
        }

        public void Move(string from, string path)
        {
            _operations.Add(new JsonPatchOperation
            {
                Operation = JsonPatchOperationType.move,
                FromPath = from,
                ParsedFromPath = PathHelper.ParsePath(from, typeof(TEntity)),
                Path = path,
                ParsedPath = PathHelper.ParsePath(path, typeof(TEntity)),
            });
        }

        public void Test(string path, object value)
        {
            _operations.Add(new JsonPatchOperation
            {
                Operation = JsonPatchOperationType.test,
                Path = path,
                ParsedPath = PathHelper.ParsePath(path, typeof(TEntity)),
                Value = value
            });
        }

        public void ApplyUpdatesTo(TEntity entity)
        {
            var preconditions = _operations.Where(operation => operation.Operation == JsonPatchOperationType.test);
            if (preconditions.Any(operation => AreNotEqual(entity, operation)))
            {
                return;
            }

            foreach (var operation in _operations)
            {
                switch (operation.Operation)
                {
                    case JsonPatchOperationType.remove:
                        PathHelper.SetValueFromPath(typeof(TEntity), operation.ParsedPath, entity, null, JsonPatchOperationType.remove);
                        break;
                    case JsonPatchOperationType.replace:
                        PathHelper.SetValueFromPath(typeof(TEntity), operation.ParsedPath, entity, operation.Value, JsonPatchOperationType.replace);
                        break;
                    case JsonPatchOperationType.add:
                        PathHelper.SetValueFromPath(typeof(TEntity), operation.ParsedPath, entity, operation.Value, JsonPatchOperationType.add);
                        break;
                    case JsonPatchOperationType.move:
                        var value = PathHelper.GetValueFromPath(typeof(TEntity), operation.ParsedFromPath, entity);
                        PathHelper.SetValueFromPath(typeof(TEntity), operation.ParsedFromPath, entity, null, JsonPatchOperationType.remove);
                        PathHelper.SetValueFromPath(typeof(TEntity), operation.ParsedPath, entity, value, JsonPatchOperationType.add);
                        break;
                    case JsonPatchOperationType.test:
                        break;
                    default:
                        throw new NotSupportedException("Operation not supported: " + operation.Operation);
                }
            }
        }

        private static bool AreNotEqual(TEntity entity, JsonPatchOperation operation)
        {
            return PathHelper.GetValueFromPath(typeof(TEntity), operation.ParsedPath, entity) != operation.Value;
        }
    }
}
