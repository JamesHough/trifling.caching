// <copyright company="James Hough">
//   Copyright (c) James Hough. Licensed under MIT License - refer to LICENSE file
// </copyright>
namespace Trifling.Caching
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for the cache engine.
    /// </summary>
    /// <remarks>These are generic configurations, some of which which may or may not apply in 
    /// certain cache engine implementations.</remarks>
    public class CacheEngineConfiguration
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="CacheEngineConfiguration"/> class. 
        /// </summary>
        /// <remarks>This constructor is typically useful for local caches that don't have a remote connection.</remarks>
        public CacheEngineConfiguration()
            : this(string.Empty, 0)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CacheEngineConfiguration"/> class with the given configuration values. 
        /// </summary>
        /// <param name="server">The remote server for the primary cache engine.</param>
        /// <param name="port">The network port for the remote primary cache engine.</param>
        /// <remarks>This constructor is typically useful for remote caches that are available via the network.</remarks>
        public CacheEngineConfiguration(string server, int port)
        {
            this.Server = server;
            this.Port = port;
            this.ConnectionTimeoutMilliseconds = 500;
            this.ResponseTimeoutMilliseconds = 1000;
        }

        /// <summary>
        /// Gets or sets the server address for a remote primary cache server.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the network port for a remote primary cache server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the timeout to connect to the cache measured in milliseconds.
        /// </summary>
        public int ConnectionTimeoutMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the timeout to wait for the cache engine to complete an operation. 
        /// The timeout value is measured in milliseconds.
        /// </summary>
        public int ResponseTimeoutMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets a string containing additional configuration options for the connection.
        /// </summary>
        /// <remarks>The value and formatting of this string is dependent on the individual
        /// implementation's requirements. Some may accept delimited key pairs while others
        /// expect a JSON string.</remarks>
        public string AdditionalConnectionOptions { get; set; }

        /// <summary>
        /// Gets or sets the alternative network servers that should be used if the primary is
        /// not reachable or was previously discovered to be offline. The use of this list is
        /// implementation-specific, but the key-value pairs represent a collection of
        /// <see cref="Server"/> and <see cref="Port"/> values.  
        /// </summary>
        public Dictionary<string, int> AlternativeServers { get; set; }
    }
}
