﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using SharpYaml.Serialization;

namespace Microsoft.OpenApi.Readers.ParseNodes
{
    internal abstract class ParseNode
    {
        protected ParseNode(ParsingContext parsingContext, OpenApiDiagnostic diagnostic)
        {
            Context = parsingContext;
            Diagnostic = diagnostic;
        }

        public ParsingContext Context { get; }

        public OpenApiDiagnostic Diagnostic { get; }

        public MapNode CheckMapNode(string nodeName)
        {
            var mapNode = this as MapNode;
            if (mapNode == null)
            {
                throw new OpenApiException($"{nodeName} must be a map/object");
            }

            return mapNode;
        }

        public static ParseNode Create(ParsingContext context, OpenApiDiagnostic diagnostic, YamlNode node)
        {
            var listNode = node as YamlSequenceNode;

            if (listNode != null)
            {
                return new ListNode(context, diagnostic, listNode);
            }

            var mapNode = node as YamlMappingNode;
            if (mapNode != null)
            {
                return new MapNode(context, diagnostic, mapNode);
            }

            return new ValueNode(context, diagnostic, node as YamlScalarNode);
        }

        public virtual List<T> CreateList<T>(Func<MapNode, T> map)
        {
            throw new OpenApiException("Cannot create list from this type of node.");
        }

        public virtual Dictionary<string, T> CreateMap<T>(Func<MapNode, T> map)
        {
            throw new OpenApiException("Cannot create map from this type of node.");
        }

        public virtual Dictionary<string, T> CreateMapWithReference<T>(
            ReferenceType referenceType,
            Func<MapNode, T> map)
            where T : class, IOpenApiReferenceable
        {
            throw new OpenApiException("Cannot create map from this reference.");
        }

        public virtual List<T> CreateSimpleList<T>(Func<ValueNode, T> map)
        {
            throw new OpenApiException("Cannot create simple list from this type of node.");
        }

        public virtual Dictionary<string, T> CreateSimpleMap<T>(Func<ValueNode, T> map)
        {
            throw new OpenApiException("Cannot create simple map from this type of node.");
        }

        public virtual IOpenApiAny CreateAny()
        {
            throw new OpenApiException("Cannot create an Any object this type of node.");
        }

        public virtual string GetRaw()
        {
            throw new OpenApiException("Cannot get raw value from this type of node.");
        }

        public virtual string GetScalarValue()
        {
            throw new OpenApiException("Cannot create a scalar value from this type of node.");
        }

        public virtual List<IOpenApiAny> CreateListOfAny()
        {
            throw new OpenApiException("Cannot create a list from this type of node.");
        }

    }
}