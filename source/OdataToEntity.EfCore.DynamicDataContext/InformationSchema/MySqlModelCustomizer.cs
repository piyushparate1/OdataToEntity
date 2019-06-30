﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.EfCore.DynamicDataContext.InformationSchema
{
    public sealed class MySqlModelCustomizer : IModelCustomizer
    {
        internal sealed class MySqlEntityMaterializerSource : EntityMaterializerSource
        {
            public override Expression CreateMaterializeExpression(IEntityType entityType, Expression materializationExpression, int[] indexMap = null)
            {
                var block = (BlockExpression)base.CreateMaterializeExpression(entityType, materializationExpression, indexMap);
                var expressions = new List<Expression>(block.Expressions);

                expressions[expressions.Count - 1] = Expression.Call(((Action<Object>)Initialize).Method, block.Variables);
                expressions.Add(block.Expressions[block.Expressions.Count - 1]);
                return Expression.Block(block.Variables, expressions);
            }
            private static void Initialize(Object entity)
            {
                if (entity is KeyColumnUsage keyColumnUsage && keyColumnUsage.ConstraintName == "PRIMARY")
                    keyColumnUsage.ConstraintName = "PK_" + keyColumnUsage.TableName;
                else if (entity is TableConstraint tableConstraint && tableConstraint.ConstraintName == "PRIMARY")
                    tableConstraint.ConstraintName = "PK_" + tableConstraint.TableName;
                else if (entity is ReferentialConstraint referentialConstraint && referentialConstraint.UniqueConstraintName == "PRIMARY")
                    referentialConstraint.UniqueConstraintName = "PK_" + referentialConstraint.ReferencedTableName;
                else if (entity is Routine routine)
                    routine.RoutineSchema = null;
            }
        }

        [Table("COLUMNS", Schema = "INFORMATION_SCHEMA")]
        internal sealed class MySqlDbGeneratedColumn
        {
            [Column("TABLE_SCHEMA")]
            public String TableSchema { get; set; }
            [Column("TABLE_NAME")]
            public String TableName { get; set; }
            [Column("COLUMN_NAME")]
            public String ColumnName { get; set; }
            [Column("EXTRA")]
            public String Extra { get; set; }
        }

        private readonly ModelCustomizer _modelCustomizer;

        public MySqlModelCustomizer(ModelCustomizerDependencies dependencies)
        {
            _modelCustomizer = new ModelCustomizer(dependencies);
        }

        public void Customize(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, DbContext context)
        {
            _modelCustomizer.Customize(modelBuilder, context);

            String databaseName = context.Database.GetDbConnection().Database;
            modelBuilder.Query<MySqlDbGeneratedColumn>();
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
                entityType.QueryFilter = GetFilter(entityType.ClrType, databaseName);

            IMutableProperty specificSchema = (Property)modelBuilder.Model.FindEntityType(typeof(Routine)).FindProperty(nameof(Routine.SpecificSchema));
            specificSchema.Relational().ColumnName = "ROUTINE_SCHEMA";

            IMutableEntityType referentialConstraint = modelBuilder.Model.FindEntityType(typeof(ReferentialConstraint));
            referentialConstraint.AddProperty(nameof(ReferentialConstraint.ReferencedTableName), typeof(String)).Relational().ColumnName = "REFERENCED_TABLE_NAME";
        }
        private static LambdaExpression GetFilter(Type entityType, String databaseName)
        {
            PropertyInfo propertyInfo = entityType.GetProperty("TableSchema");
            if (propertyInfo == null)
            {
                propertyInfo = entityType.GetProperty("ConstraintSchema");
                if (propertyInfo == null)
                    propertyInfo = entityType.GetProperty("SpecificSchema");
            }

            ParameterExpression parameter = Expression.Parameter(entityType);
            MemberExpression property = Expression.Property(parameter, propertyInfo);

            BinaryExpression filter = Expression.Equal(property, Expression.Constant(databaseName));
            return Expression.Lambda(filter, parameter);
        }
    }
}