using System.Runtime.CompilerServices;
using VectorRagDemo.BLL.Processors;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Services
{
    public abstract class ServiceBase
    {
        public VectorDbContext Context;
        public LogboekDbContext LogboekContext;
        public IConfiguration Configuration;

        private HttpClient _httpClient;
        public HttpClient HttpClient
        {
            get
            {
                if(_httpClient == null)
                {
                    _httpClient = new HttpClient();
                }
                return _httpClient;
            }
            set
            {
                _httpClient = value;
            }
        }


        private GeminiProcessor _geminiProcessor;

        public GeminiProcessor GeminiProcessor
        {
            get
            {
                if(_geminiProcessor == null)
                {
                    _geminiProcessor = new GeminiProcessor(Context, HttpClient, LogboekContext);
                }
                return _geminiProcessor;
            }
            set
            {
                _geminiProcessor = value;
            }
        }

        private EmbeddingProcessor _embeddingProcessor;

        public EmbeddingProcessor EmbeddingProcessor
        {
            get
            {
                if (_embeddingProcessor == null)
                {
                    _embeddingProcessor = new EmbeddingProcessor(HttpClient, LogboekContext);
                }
                return _embeddingProcessor;
            }
            set
            {
                _embeddingProcessor = value;
            }
        }

        private VectorQueryProcessor _vectorQueryProcessor;

        public VectorQueryProcessor VectorQueryProcessor
        {
            get
            {
                if (_vectorQueryProcessor == null)
                {
                    _vectorQueryProcessor = new VectorQueryProcessor(Context);
                }
                return _vectorQueryProcessor;
            }
            set
            {
                _vectorQueryProcessor = value;
            }
        }

        private PreProcessingProcessor _preProcessingProcessor;

        public PreProcessingProcessor PreProcessingProcessor
        {
            get
            {
                if (_preProcessingProcessor == null)
                {
                    _preProcessingProcessor = new PreProcessingProcessor(HttpClient, Context, LogboekContext);
                }
                return _preProcessingProcessor;
            }
            set
            {
                _preProcessingProcessor = value;
            }
        }

        public ServiceBase(VectorDbContext context, LogboekDbContext logboekContext, IConfiguration configuration)
        {
            Context = context;
            LogboekContext = logboekContext;
            Configuration = configuration;
        }
    }
}
