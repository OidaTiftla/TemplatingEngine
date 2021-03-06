﻿using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TemplatingEngine.Engines {

    public class T4Engine : IEngine {

        #region implement IEngine

        /// <summary>
        /// Add namespace for using directives
        /// </summary>
        /// <param name="ns">namespace</param>
        public void AddUsing(string ns) {
            //if (!this.usings_.Contains(ns))
            //    this.usings_.Add(ns);
            throw new System.NotImplementedException();
        }

        public string Generate<T>(string template, T context) {
            throw new System.NotImplementedException();
        }

        public void Generate<T>(Stream template, T context, Stream output) {
            throw new System.NotImplementedException();
        }

        public async Task<string> GenerateAsync<T>(string template, T context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.Generate(template, context);
            });
            return output;
        }

        public async void GenerateAsync<T>(Stream template, T context, Stream output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.Generate(template, context, output);
            });
        }

        public string GenerateDynamic(string template, dynamic context) {
            throw new System.NotImplementedException();
        }

        public void GenerateDynamic(Stream template, dynamic context, Stream output) {
            throw new System.NotImplementedException();
        }

        public async Task<string> GenerateDynamicAsync(string template, dynamic context) {
            string output = null;
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                output = this.GenerateDynamic(template, context);
            });
            return output;
        }

        public async void GenerateDynamicAsync(Stream template, dynamic context, Stream output) {
            await Task.Run(() => {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                this.GenerateDynamic(template, context, output);
            });
        }

        #endregion implement IEngine
    }
}