﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Xunit;

namespace Microsoft.OpenApi.Tests
{
    using System.IO;

    using Microsoft.OpenApi.Readers;

    public class OpenApiWorkspaceTests
    {
        [Fact]
        public void OpenApiWorkspaceCanHoldMultipleDocuments()
        {
            var workspace = new OpenApiWorkspace();

            workspace.AddDocument("root", new OpenApiDocument());
            workspace.AddDocument("common", new OpenApiDocument());

            Assert.Equal(2, workspace.Documents.Count());
        }

        [Fact]
        public void OpenApiWorkspacesAllowDocumentsToReferenceEachOther()
        {
            var workspace = new OpenApiWorkspace();

            workspace.AddDocument("root", new OpenApiDocument() {
                Paths = new OpenApiPaths()
                {
                    ["/"] = new OpenApiPathItem()
                    {
                        Operations  = new Dictionary<OperationType, OpenApiOperation>()
                        {
                            [OperationType.Get] = new OpenApiOperation() {
                                Responses = new OpenApiResponses()
                                {
                                    ["200"] = new OpenApiResponse()
                                    {
                                       Content = new Dictionary<string,OpenApiMediaType>()
                                       {
                                           ["application/json"] = new OpenApiMediaType()
                                           {
                                               Schema = new OpenApiSchema()
                                               {
                                                   Reference = new OpenApiReference()
                                                   {
                                                       Id = "test",
                                                       Type = ReferenceType.Schema
                                                   }
                                               }
                                           }
                                       }
                                    }
                                }
                            }
                        }
                    } 
                }
            });
            workspace.AddDocument("common", new OpenApiDocument() {
                Components = new OpenApiComponents()
                {
                    Schemas = {
                        ["test"] = new OpenApiSchema() {
                            Type = "string",
                            Description = "The referenced one"
                        }
                    }
                }
            });

            Assert.Equal(2, workspace.Documents.Count());
        }

        [Fact]
        public void OpenApiWorkspacesCanResolveExternalReferences()
        {
            var workspace = new OpenApiWorkspace();
            workspace.AddDocument("common", CreateCommonDocument());
            var schema = workspace.ResolveReference(new OpenApiReference()
            {
                Id = "test",
                Type = ReferenceType.Schema,
                ExternalResource ="common"
            }) as OpenApiSchema;

            Assert.NotNull(schema);
            Assert.Equal("The referenced one", schema.Description);
        }

        [Fact]
        public void OpenApiWorkspacesAllowDocumentsToReferenceEachOther_short()
        {
            var workspace = new OpenApiWorkspace();

            var doc = new OpenApiDocument();
            doc.CreatePathItem("/", p =>
            {
                p.Description = "Consumer";
                p.CreateOperation(OperationType.Get, op =>
                  op.CreateResponse("200", re =>
                  {
                      re.Description = "Success";
                      re.CreateContent("application/json", co =>
                          co.Schema = new OpenApiSchema()
                          {
                              Reference = new OpenApiReference()  // Reference 
                              {
                                  Id = "test",
                                  Type = ReferenceType.Schema,
                                  ExternalResource = "common"
                              },
                              UnresolvedReference = true
                          }
                      );
                  })
                );
            });

            workspace.AddDocument("root", doc);

            workspace.AddDocument("common", CreateCommonDocument());

            doc.ResolveReferences(true);

            var schema = doc.Paths["/"].Operations[OperationType.Get].Responses["200"].Content["application/json"].Schema;
            Assert.False(schema.UnresolvedReference);
        }


        [Fact]
        public void OpenApiWorkspacesShouldNormalizeDocumentLocations()
        {
            Assert.True(false);
        }

        [Fact]
        public void CanMergeMultipleAzureFunctionDocumentsIntoADocumentThatCanBeImportedIntoAPIM()
        {
            var documents = new[]
            {
                @"Workspaces\Docs\Marain.Hosting.Text.Scoring.ExponentialNormalizerScore.Post.json",
                @"Workspaces\Docs\Marain.Hosting.Text.Scoring.LinearNormalizerScore.Post.json",
                @"Workspaces\Docs\Marain.Hosting.Text.Scoring.Score.Post.json",
                @"Workspaces\Docs\Marain.Hosting.Text.Scoring.ScoringTypeDescription.Get.json",
                @"Workspaces\Docs\Marain.Hosting.Text.Scoring.Strategies.Get.json",
            };

            var workspace = new OpenApiWorkspace();

            foreach (var document in documents)
            {
                var openApiDocument = new OpenApiStreamReader().Read(File.OpenRead(document.ResolveBaseDirectory()), out var context);

                workspace.AddDocument(Guid.NewGuid().ToString(), openApiDocument);
            }

            Assert.True(true);
        }

        // Enable Workspace to load from any reader, not just streams.

        // Test fragments

        // Test artifacts

        private static OpenApiDocument CreateCommonDocument()
        {
            return new OpenApiDocument()
            {
                Components = new OpenApiComponents()
                {
                    Schemas = {
                        ["test"] = new OpenApiSchema() {
                            Type = "string",
                            Description = "The referenced one"
                        }
                    }
                }
            };
        }
    }




    public static class OpenApiFactoryExtensions {

    public static OpenApiDocument CreatePathItem(this OpenApiDocument document, string path, Action<OpenApiPathItem> config)
    {
        var pathItem = new OpenApiPathItem();
        config(pathItem);
        document.Paths.Add(path, pathItem);
        return document;
    }

    public static OpenApiPathItem CreateOperation(this OpenApiPathItem parent, OperationType opType, Action<OpenApiOperation> config)
    {
        var child = new OpenApiOperation();
        config(child);
        parent.Operations.Add(opType, child);
        return parent;
    }

    public static OpenApiOperation CreateResponse(this OpenApiOperation parent, string status, Action<OpenApiResponse> config)
    {
        var child = new OpenApiResponse();
        config(child);
        parent.Responses.Add(status, child);
        return parent;
    }

    public static OpenApiResponse CreateContent(this OpenApiResponse parent, string mediaType, Action<OpenApiMediaType> config)
    {
        var child = new OpenApiMediaType();
        config(child);
        parent.Content.Add(mediaType, child);
        return parent;
    }

}
}
