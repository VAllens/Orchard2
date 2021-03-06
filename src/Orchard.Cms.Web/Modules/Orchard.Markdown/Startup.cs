﻿using Microsoft.AspNetCore.Mvc.Modules;
using Microsoft.Extensions.DependencyInjection;
using Orchard.Markdown.Drivers;
using Orchard.Markdown.Handlers;
using Orchard.Markdown.Indexing;
using Orchard.Markdown.Model;
using Orchard.Markdown.Settings;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Display.ContentDisplay;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentTypes.Editors;
using Orchard.Data.Migration;
using Orchard.Indexing;
using Orchard.Tokens;

namespace Orchard.Markdown
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // Markdown Part
            services.AddScoped<IContentPartDisplayDriver, MarkdownPartDisplay>();
            services.AddSingleton<ContentPart, MarkdownPart>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, MarkdownPartSettingsDisplayDriver>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentPartIndexHandler, MarkdownPartIndexHandler>();
            services.AddScoped<IContentPartHandler, MarkdownPartHandler>();

            services.AddNullTokenizer();
        }
    }
}
