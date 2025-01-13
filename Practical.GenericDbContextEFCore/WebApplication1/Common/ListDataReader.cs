using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Practical.GenericDbContextEFCore.Common
{
    /// <summary>
    /// Support fast load data to sql server
    /// </summary>
    /// <summary>
    ///     IDataReader that can be used for "reading" an IEnumerable<T> collection
    /// </summary>
    public class ListDataReader<T> : IDataReader
    {
        private readonly List<BaseField> _fields = new List<BaseField>();
        private T _currentElement;
        private IEnumerator<T> _enumerator;
        private bool _enumeratorState;

        #region IDisposable Members

        public void Dispose()
        {
            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
                _currentElement = default(T);
                _enumeratorState = false;
            }
            _closed = true;
        }

        #endregion

        #region IDataReader Members

        private bool _closed;

        /// <summary>
        /// Impelement Close from IDataReader base original ListDataReader
        /// </summary>
        public void Close()
        {
            _closed = true;
        }

        /// <summary>
        /// Impelement Depth from IDataReader base original ListDataReader
        /// </summary>
        public int Depth
        {
            get { return 0; }
        }

        /// <summary>
        /// Impelement Depth from IDataReader base original ListDataReader
        /// </summary>
        /// <returns></returns>
        public DataTable GetSchemaTable()
        {
            var dt = new DataTable();
            foreach (BaseField field in _fields)
            {
                dt.Columns.Add(new DataColumn(field.Name, field.Type));
            }
            return dt;
        }

        public bool IsClosed
        {
            get { return _closed; }
        }

        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Impelement Read from IDataReader base original ListDataReader
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Read()
        {
            if (IsClosed)
                throw new InvalidOperationException("DataReader is closed");
            _enumeratorState = _enumerator.MoveNext();
            _currentElement = _enumeratorState ? _enumerator.Current : default(T);
            return _enumeratorState;
        }

        /// <summary>
        /// Impelement RecordsAffected from IDataReader base original ListDataReader
        /// </summary>
        public int RecordsAffected
        {
            get { return -1; }
        }

        #endregion

        #region IDataRecord Members

        /// <summary>
        /// Impelement RecordsAffected from IDataReader base original ListDataReader
        /// </summary>
        public int FieldCount
        {
            get { return _fields.Count; }
        }

        /// <summary>
        /// Impelement GetFieldType from IDataReader base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>Type</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public Type GetFieldType(int i)
        {
            if (i < 0 || i >= _fields.Count)
                throw new IndexOutOfRangeException();
            return _fields[i].Type;
        }

        /// <summary>
        /// Impelement GetDataTypeName from IDataReader base original ListDataReader
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        /// <summary>
        ///  Impelement GetName from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public string GetName(int i)
        {
            if (i < 0 || i >= _fields.Count)
                throw new IndexOutOfRangeException();
            return _fields[i].Name;
        }

        /// <summary>
        ///  Impelement GetName from base original ListDataReader
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public int GetOrdinal(string name)
        {
            for (int i = 0; i < _fields.Count; i++)
                if (_fields[i].Name == name)
                    return i;
            throw new IndexOutOfRangeException(name);
        }

        /// <summary>
        /// Impelement GetDataTypeName from IDataReader base original ListDataReader 
        /// </summary>
        /// <param name="i"></param>
        /// <returns>bool</returns>
        public bool IsDBNull(int i)
        {
            return GetValue(i) == null;
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="name"></param>
        /// <returns>object</returns>
        public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>object</returns>
        public object this[int i]
        {
            get { return GetValue(i); }
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>object</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public object GetValue(int i)
        {
            if (IsClosed || !_enumeratorState)
                throw new InvalidOperationException("DataReader is closed or has reached the end of the enumerator");
            if (i < 0 || i >= _fields.Count)
                throw new IndexOutOfRangeException();
            return _fields[i].GetValue(_currentElement);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="values">object[]</param>
        /// <returns>int</returns>
        public int GetValues(object[] values)
        {
            int length = Math.Min(_fields.Count, values.Length);
            for (int i = 0; i < length; i++)
                values[i] = GetValue(i);
            return length;
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns></returns>
        public bool GetBoolean(int i)
        {
            return (bool)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>byte</returns>
        public byte GetByte(int i)
        {
            return (byte)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>char</returns>
        public char GetChar(int i)
        {
            return (char)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>DateTime</returns>
        public DateTime GetDateTime(int i)
        {
            return (DateTime)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>decimal</returns>
        public decimal GetDecimal(int i)
        {
            return (decimal)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>double</returns>
        public double GetDouble(int i)
        {
            return (double)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>float</returns>
        public float GetFloat(int i)
        {
            return (float)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>Guid</returns>
        public Guid GetGuid(int i)
        {
            return (Guid)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>sort</returns>
        public short GetInt16(int i)
        {
            return (short)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>int</returns>
        public int GetInt32(int i)
        {
            return (int)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>long</returns>
        public long GetInt64(int i)
        {
            return (long)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>string</returns>
        public string GetString(int i)
        {
            return (string)GetValue(i);
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <param name="fieldOffset">long</param>
        /// <param name="buffer">byte[]</param>
        /// <param name="bufferoffset">int</param>
        /// <param name="length">int</param>
        /// <returns>long</returns>
        /// <exception cref="NotSupportedException"></exception>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <param name="fieldoffset">long</param>
        /// <param name="buffer">char[]</param>
        /// <param name="bufferoffset">int</param>
        /// <param name="length">int</param>
        /// <returns>long</returns>
        /// <exception cref="NotSupportedException"></exception>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Impelement from base original ListDataReader
        /// </summary>
        /// <param name="i">int</param>
        /// <returns>IDataReader</returns>
        /// <exception cref="NotSupportedException"></exception>
        public IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Helper Classes

        private abstract class BaseField
        {
            private ConcurrentDictionary<string, Func<T, object>> m_GetterDictionary =
                new ConcurrentDictionary<string, Func<T, object>>();

            public abstract Type Type { get; }
            public abstract string Name { get; }
            public abstract object GetValue(T instance);

            protected void AddGetter(Type classType, string fieldName, Func<T, object> getter)
            {
                m_GetterDictionary.TryAdd(string.Concat(classType.FullName, fieldName), getter);
            }

            protected Func<T, object> GetGetter(Type classType, string fieldName)
            {
                Func<T, object> getter = null;
                if (m_GetterDictionary.TryGetValue(string.Concat(classType.FullName, fieldName), out getter))
                    return getter;
                return null;
            }
        }

        private class Field : BaseField
        {
            private readonly Func<T, object> _dynamicGetter;
            private readonly FieldInfo _info;

            public Field(FieldInfo info)
            {
                _info = info;
                _dynamicGetter = CreateGetMethod(info);
            }

            public override Type Type
            {
                get { return _info.FieldType; }
            }

            public override string Name
            {
                get { return _info.Name; }
            }

            public override object GetValue(T instance)
            {
                //return m_Info.GetValue(instance); // Reflection is slow
                return _dynamicGetter(instance);
            }

            // Create dynamic method for faster access instead via reflection
            private Func<T, object> CreateGetMethod(FieldInfo fieldInfo)
            {
                Type classType = typeof(T);
                Func<T, object> dynamicGetter = GetGetter(classType, fieldInfo.Name);
                if (dynamicGetter == null)
                {
                    ParameterExpression instance = Expression.Parameter(classType);
                    MemberExpression property = Expression.Field(instance, fieldInfo);
                    UnaryExpression convert = Expression.Convert(property, typeof(object));
                    dynamicGetter = (Func<T, object>)Expression.Lambda(convert, instance).Compile();
                    AddGetter(classType, fieldInfo.Name, dynamicGetter);
                }

                return dynamicGetter;
            }
        }

        private class Property : BaseField
        {
            private readonly Func<T, object> _dynamicGetter;
            private readonly PropertyInfo _info;

            public Property(PropertyInfo info)
            {
                _info = info;
                _dynamicGetter = CreateGetMethod(info);
            }

            public override Type Type
            {
                get { return _info.PropertyType; }
            }

            public override string Name
            {
                get { return _info.Name; }
            }

            public override object GetValue(T instance)
            {
                //return m_Info.GetValue(instance, null); // Reflection is slow
                return _dynamicGetter(instance);
            }

            // Create dynamic method for faster access instead via reflection
            private Func<T, object> CreateGetMethod(PropertyInfo propertyInfo)
            {
                Type classType = typeof(T);
                Func<T, object> dynamicGetter = GetGetter(classType, propertyInfo.Name);
                if (dynamicGetter == null)
                {
                    ParameterExpression instance = Expression.Parameter(classType);
                    MemberExpression property = Expression.Property(instance, propertyInfo);
                    UnaryExpression convert = Expression.Convert(property, typeof(object));
                    dynamicGetter = (Func<T, object>)Expression.Lambda(convert, instance).Compile();
                    AddGetter(classType, propertyInfo.Name, dynamicGetter);
                }

                return dynamicGetter;
            }
        }

        private class Self : BaseField
        {
            private readonly Type _type;

            public Self()
            {
                _type = typeof(T);
            }

            public override Type Type
            {
                get { return _type; }
            }

            public override string Name
            {
                get { return string.Empty; }
            }

            public override object GetValue(T instance)
            {
                return instance;
            }
        }

        #endregion

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="collection">The collection to be read</param>
        /// <param name="fields">
        ///     The list of public field/properties to read from each T (in order), OR if no fields are given only
        ///     one field will be available: T itself
        /// </param>
        public ListDataReader(IEnumerable<T> collection, params string[] fields)
        {

            if (collection == null)
                throw new ArgumentNullException("Collection");

            _enumerator = collection.GetEnumerator();

            if (_enumerator == null)
                throw new NullReferenceException("Collection does not implement GetEnumerator");

            SetFields(fields);
        }

        /// <summary>
        /// Impelement from base original ListDataReader and add new BindingFlags.DeclaredOnly for datacontextV4
        /// </summary>
        /// <param name="fields"></param>
        /// <exception cref="NullReferenceException"></exception>
        private void SetFields(ICollection<string> fields)
        {
            if (fields.Count > 0)
            {
                Type type = typeof(T);
                foreach (string field in fields)
                {
                    PropertyInfo pInfo = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (pInfo == null && type.BaseType != null)
                    {
                        // entity inherit from BaseEntityWithDefaultColumns, should not apply DeclaredOnly
                        pInfo = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    }

                    if (pInfo != null)
                        _fields.Add(new Property(pInfo));
                    else
                    {
                        FieldInfo fInfo = type.GetField(field);
                        if (fInfo != null)
                            _fields.Add(new Field(fInfo));
                        else
                            throw new NullReferenceException(
                                string.Format(
                                    "EnumerableDataReader<T>: Missing property or field '{0}' in Type '{1}'.", field,
                                    type.Name));
                    }
                }
            }
            else
                _fields.Add(new Self());
        }
    }
}
