using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Practical.GenericDbContextEFCore.Extension
{
    public static class ObjectExtension
    {
        /// <summary>
        /// Try mapping entity to model with same name
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="entities"></param>
        /// <param name="customMapperFunc"></param>
        /// <returns></returns>
        public static List<TModel> MappEntityToModel<TEntity, TModel>(this List<TEntity> entities, Func<string, TEntity, TModel, bool> customMapperFunc = null)
        {
            List<TModel> models = new List<TModel>();
            TModel obj = default(TModel);
            var props = typeof(TEntity).GetProperties().Select(x => x.Name);
            foreach (var entity in entities)
            {
                obj = Activator.CreateInstance<TModel>();
                if (obj != null)
                {
                    foreach (PropertyInfo prop in obj.GetType().GetProperties())
                    {
                        if (prop.CustomAttributes != null && prop.CustomAttributes.Any(x => x.AttributeType == typeof(NotMappedAttribute) || x.AttributeType == typeof(ReadOnlyAttribute))) continue;

                        if (prop.PropertyType.Namespace != nameof(System)) continue;

                        if (props.Any(x => string.Equals(x, prop.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            bool haCustomMapped = false;
                            var desValue = entity.GetValueIgnoreCase(prop.Name);
                            if (customMapperFunc != null)
                            {
                                haCustomMapped = customMapperFunc.Invoke(prop.Name, entity, obj);
                            }
                            if (!haCustomMapped)
                            {
                                prop.SetValue(obj, desValue, null);
                            }
                        }
                    }
                }
                models.Add(obj);
            }

            return models;
        }

        /// <summary>
        /// Check list is null or empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || !list.Any();
        }

        /// <summary>
        /// Get object value ignore case
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static object GetValueIgnoreCase(this Object obj, string propName)
        {
            return obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj, null);
        }

        public static string GetPropertyName<T>(this Expression<Func<T, object>> property)
        {
            try {
                var lambda = (LambdaExpression)property;
                MemberExpression memberExpression;

                if (lambda.Body is UnaryExpression) {
                    UnaryExpression unaryExpression = (UnaryExpression)lambda.Body;
                    memberExpression = (MemberExpression)unaryExpression.Operand;
                }
                else {
                    memberExpression = (MemberExpression)lambda.Body;
                }

                return memberExpression.Member.Name;
            }
            catch (Exception ex) {
                throw ex;
            }
        }
    }
}
