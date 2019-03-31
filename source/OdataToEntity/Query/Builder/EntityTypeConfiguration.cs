﻿using Microsoft.OData.Edm;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.Query.Builder
{
    public sealed class EntityTypeConfiguration<TEntityType> where TEntityType : class
    {
        private readonly IEdmEntityType _entityType;
        private readonly OeModelBoundFluentBuilder _modelBuilder;

        internal EntityTypeConfiguration(OeModelBoundFluentBuilder modelBuilder, IEdmEntityType entityType)
        {
            _modelBuilder = modelBuilder;
            _entityType = entityType;
        }

        public EntityTypeConfiguration<TEntityType> Count(QueryOptionSetting setting)
        {
            _modelBuilder.ModelBoundSettingsBuilder.SetCount(setting == QueryOptionSetting.Allowed, _entityType);
            return this;
        }
        public EntityTypeConfiguration<TEntityType> Expand(SelectExpandType expandType, params String[] propertyNames)
        {
            return Select(expandType, propertyNames);
        }
        public EntityTypeConfiguration<TEntityType> Filter(QueryOptionSetting setting)
        {
            foreach (IEdmProperty edmProperty in _entityType.Properties())
                _modelBuilder.ModelBoundSettingsBuilder.SetFilter(edmProperty, setting == QueryOptionSetting.Allowed);
            return this;
        }
        public EntityTypeConfiguration<TEntityType> Filter(QueryOptionSetting setting, params String[] propertyNames)
        {
            foreach (String propertyName in propertyNames)
            {
                IEdmProperty edmProperty = _entityType.GetPropertyIgnoreCase(propertyName);
                _modelBuilder.ModelBoundSettingsBuilder.SetFilter(edmProperty, setting == QueryOptionSetting.Allowed);
            }
            return this;
        }
        public EntityTypeConfiguration<TEntityType> OrderBy(QueryOptionSetting setting)
        {
            foreach (IEdmProperty edmProperty in _entityType.Properties())
                _modelBuilder.ModelBoundSettingsBuilder.SetOrderBy(edmProperty, setting == QueryOptionSetting.Allowed);
            return this;
        }
        public EntityTypeConfiguration<TEntityType> OrderBy(QueryOptionSetting setting, params String[] propertyNames)
        {
            foreach (String propertyName in propertyNames)
            {
                IEdmProperty edmProperty = _entityType.GetPropertyIgnoreCase(propertyName);
                _modelBuilder.ModelBoundSettingsBuilder.SetOrderBy(edmProperty, setting == QueryOptionSetting.Allowed);
            }
            return this;
        }
        public EntityTypeConfiguration<TEntityType> Page(int? maxTopValue, int? pageSizeValue)
        {
            _modelBuilder.ModelBoundSettingsBuilder.SetMaxTop(maxTopValue.GetValueOrDefault(), _entityType);
            _modelBuilder.ModelBoundSettingsBuilder.SetPageSize(pageSizeValue.GetValueOrDefault(), _entityType);
            return this;
        }
        public PropertyConfiguration Property(Expression<Func<TEntityType, Object>> propertyExpression)
        {
            var property = (MemberExpression)propertyExpression.Body;
            var propertyInfo = (PropertyInfo)property.Member;
            IEdmProperty edmProperty = _entityType.GetPropertyIgnoreCase(propertyInfo.Name);
            return new PropertyConfiguration(_modelBuilder, edmProperty);
        }
        public EntityTypeConfiguration<TEntityType> Select(SelectExpandType expandType, params String[] propertyNames)
        {
            foreach (String propertyName in propertyNames)
            {
                IEdmProperty edmProperty = _entityType.GetPropertyIgnoreCase(propertyName);
                _modelBuilder.ModelBoundSettingsBuilder.SetSelect(edmProperty, expandType);
            }
            return this;
        }
    }

    public sealed class EntitySetConfiguration<TEntityType> where TEntityType : class
    {
        private readonly OeModelBoundFluentBuilder _modelBuilder;

        internal EntitySetConfiguration(OeModelBoundFluentBuilder modelBuilder, IEdmEntitySet entitySet)
        {
            _modelBuilder = modelBuilder;

            EntityType = new EntityTypeConfiguration<TEntityType>(_modelBuilder, entitySet.EntityType());
        }

        public EntityTypeConfiguration<TEntityType> EntityType { get; }
    }

}