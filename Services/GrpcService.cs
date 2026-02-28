using Grpc.Core;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Services
{
    public class GrpcService : VectorDbService.VectorDbServiceBase
    {
        private readonly VectorDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GrpcService> _logger;

        public GrpcService(
            VectorDbContext context,
            IConfiguration configuration,
            ILogger<GrpcService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public override async Task<SearchResponse> SearchSimilarChunks(
            SearchRequest request,
            ServerCallContext context)
        {
            var response = new SearchResponse();

            try
            {
                AppLogger.Log($"gRPC SearchSimilarChunks: {request.QueryVector.Count} dimensions, projectId={request.ProjectId}", source: nameof(GrpcService));
                // Convert the query vector to a string format for SQL
                var vectorString = "[" + string.Join(",", request.QueryVector) + "]";

                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // SQL query using pgvector cosine distance operator <=>
                var sql = @"
                    SELECT
                        c.id,
                        c.bronid,
                        c.tekst,
                        b.title as brontitle,
                        c.tekstvector <=> @QueryVector::vector as distance
                    FROM chunk c
                    INNER JOIN bron b ON c.bronid = b.id
                    WHERE c.status = 1";

                if (request.ProjectId > 0)
                {
                    sql += " AND b.project = @ProjectId";
                }

                sql += " ORDER BY distance ASC LIMIT @TopK";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TopK", request.TopK);
                command.Parameters.AddWithValue("@QueryVector", vectorString);

                if (request.ProjectId > 0)
                {
                    command.Parameters.AddWithValue("@ProjectId", request.ProjectId);
                }

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var result = new ChunkResult
                    {
                        Id = reader.GetInt32(0),
                        BronId = reader.GetInt32(1),
                        Tekst = reader.GetString(2),
                        BronTitle = reader.GetString(3),
                        SimilarityScore = 1 - reader.GetFloat(4) // Convert distance to similarity
                    };

                    response.Results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar chunks");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }

            return response;
        }

        public override async Task<AddChunkResponse> AddChunk(
            AddChunkRequest request,
            ServerCallContext context)
        {
            try
            {
                AppLogger.Log($"gRPC AddChunk: inserting vector with {request.TekstVector.Count} dimensions", source: nameof(GrpcService));
                var vectorString = "[" + string.Join(",", request.TekstVector) + "]";
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO chunk (bronid, tekst, tekstvector, gemaaktop, status)
                    VALUES (@BronID, @Tekst, @TekstVector::vector, NOW(), @Status)
                    RETURNING id;";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BronID", request.BronId);
                command.Parameters.AddWithValue("@Tekst", request.Tekst);
                command.Parameters.AddWithValue("@TekstVector", vectorString);
                command.Parameters.AddWithValue("@Status", request.Status);

                var newId = (int)await command.ExecuteScalarAsync();

                return new AddChunkResponse { Id = newId, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding chunk");
                return new AddChunkResponse { Id = 0, Success = false };
            }
        }

        public override async Task<AddBronResponse> AddBron(
            AddBronRequest request,
            ServerCallContext context)
        {
            try
            {
                var bron = new Models.Entities.Bron
                {
                    Title = request.Title,
                    Project = request.Project,
                    GemaaktOp = DateTime.Now,
                    Status = request.Status
                };

                _context.Bronnen.Add(bron);
                await _context.SaveChangesAsync();

                return new AddBronResponse { Id = bron.ID, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding bron");
                return new AddBronResponse { Id = 0, Success = false };
            }
        }

        public override async Task<AddProjectResponse> AddProject(
            AddProjectRequest request,
            ServerCallContext context)
        {
            try
            {
                var project = new Project
                {
                    Naam = request.Naam,
                    GemaaktOp = DateTime.Now,
                    Status = request.Status
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                return new AddProjectResponse { Id = project.ID, Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding project");
                return new AddProjectResponse { Id = 0, Success = false };
            }
        }

        public override async Task<ChunkResponse> GetChunkById(
            GetChunkRequest request,
            ServerCallContext context)
        {
            try
            {
                var chunk = await _context.Chunks
                    .FirstOrDefaultAsync(c => c.ID == request.Id);

                if (chunk == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Chunk not found"));
                }

                return new ChunkResponse
                {
                    Id = chunk.ID,
                    BronId = chunk.BronID,
                    Tekst = chunk.Tekst,
                    Status = chunk.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chunk");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<GetProjectForUserResponse> GetProjectForUser(
            GetProjectForUserRequest request,
            ServerCallContext context)
        {
            try
            {
                var entry = await _context.GebruikerProjecten
                    .Where(gp => gp.Gebruiker == request.GebruikerId && gp.Status == 1)
                    .FirstOrDefaultAsync();

                if (entry == null)
                    return new GetProjectForUserResponse { ProjectId = 0, Found = false };

                return new GetProjectForUserResponse { ProjectId = entry.Project, Found = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project for user");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<GetScrapingElementsResponse> GetScrapingElementsByProject(
            GetScrapingElementsRequest request,
            ServerCallContext context)
        {
            var response = new GetScrapingElementsResponse();

            try
            {
                var elements = await _context.ScrapingElement
                    .Where(e => e.Project == request.ProjectId && e.Status == 1)
                    .OrderBy(e => e.SortOrder)
                    .ToListAsync();

                foreach (var element in elements)
                {
                    response.Elements.Add(new ScrapingElementMessage
                    {
                        Id = element.ID,
                        Project = element.Project,
                        ElementName = element.ElementName ?? string.Empty,
                        Selector = element.Selector ?? string.Empty,
                        AttributeName = element.AttributeName ?? string.Empty,
                        IsRequired = element.IsRequired,
                        DefaultValue = element.DefaultValue ?? string.Empty,
                        SortOrder = element.SortOrder,
                        Status = element.Status
                    });
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scraping elements");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
    }
}