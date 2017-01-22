// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching
{
    using Trifling.Caching.Interfaces;
    using Trifling.Compression;

    /// <summary>
    /// Configuration options typically used by implementations of <see cref="ICacheManager"/>. 
    /// </summary>
    public class CacheManagerConfiguration
    {
        /// <summary>
        /// Indicates whether or not the cache manager should use G-Zip compression when 
        /// writing/reading from the cache engine. 
        /// </summary>
        private bool _useGzipCompression = false;

        /// <summary>
        /// Indicates whether or not the cache manager should use Deflate compression when 
        /// writing/reading from the cache engine. 
        /// </summary>
        private bool _useDeflateCompression = false;

        /// <summary>
        /// Gets or sets a value indicating whether the cache manager should use G-Zip compression
        /// when writing/reading from the cache engine
        /// </summary>
        public bool UseGzipCompression
        {
            get
            {
                return this._useGzipCompression;
            }

            set
            {
                // if we are setting G-Zip to true then Deflate must be false.
                if (this._useDeflateCompression && value)
                {
                    this._useDeflateCompression = false;
                }

                this._useGzipCompression = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the cache manager should use Deflate compression
        /// when writing/reading from the cache engine
        /// </summary>
        public bool UseDeflateCompression
        {
            get
            {
                return this._useDeflateCompression;
            }

            set
            {
                // if we are setting Deflate to true then G-Zip must be false.
                if (this._useGzipCompression && value)
                {
                    this._useGzipCompression = false;
                }

                this._useDeflateCompression = value;
            }
        }

        /// <summary>
        /// Gets or sets the Compression Configuration to use. If not given then a default configuration
        /// will be used. If neither the <see cref="UseDeflateCompression"/> nor the <see cref="UseGzipCompression"/>
        /// is set then this configuration value will be ignored.
        /// </summary>
        public CompressorConfiguration CompressionConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the cache engine configuration that should be used by the caching engine.
        /// </summary>
        public CacheEngineConfiguration CacheEngineConfiguration { get; set; }
    }
}
