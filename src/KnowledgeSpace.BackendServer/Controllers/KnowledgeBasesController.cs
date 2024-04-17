using KnowledgeSpace.BackendServer.Authorization;
using KnowledgeSpace.BackendServer.Constants;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.BackendServer.Extensions;
using KnowledgeSpace.BackendServer.Helpers;
using KnowledgeSpace.BackendServer.Services;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Contents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class KnowledgeBasesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ISequenceService _sequenceService;
        private readonly IStorageService _storageService;

        public KnowledgeBasesController(ApplicationDbContext context, ISequenceService sequenceService, IStorageService storageService)
        {
            _context = context;
            _sequenceService = sequenceService;
            _storageService = storageService;
        }

        #region KnowledgeBases
        [HttpPost]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.CREATE)]
        [ApiValidationFilter]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PostKnowledgeBase([FromForm] KnowledgeBaseCreateRequest request)
        {
            var knowledgeBase = new KnowledgeBase()
            {
                CategoryId = request.CategoryId,
                Title = request.Title,
                SeoAlias = request.SeoAlias,
                Description = request.Description,
                Environment = request.Environment,
                Problem = request.Problem,
                StepToReproduce = request.StepToReproduce,
                ErrorMessage = request.ErrorMessage,
                Workaround = request.Workaround,
            };
            if (request.Labels?.Length > 0)
            {
                knowledgeBase.Labels = string.Join(',', request.Labels);
            }
            knowledgeBase.OwnerUserId = User.GetUserId();
            if (string.IsNullOrEmpty(knowledgeBase.SeoAlias))
            {
                knowledgeBase.SeoAlias = TextHelper.ToUnsignString(knowledgeBase.Title);
            }
            knowledgeBase.Id = await _sequenceService.GetKnowledgeBaseId();

            //Process attachment
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var attachment in request.Attachments)
                {
                    var attachmentEntity = await SaveFile(knowledgeBase.Id, attachment);
                    _context.Attachments.Add(attachmentEntity);
                }
            }

            _context.KnowledgeBases.Add(knowledgeBase);

            //Process label
            if (request.Labels?.Length > 0)
            {
                await ProcessLabel(request, knowledgeBase);
            }

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { id = knowledgeBase.Id });
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpGet]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetKnowledgeBases()
        {
            var knowledgeBases = _context.KnowledgeBases;

            var knowledgeBaseVms = await knowledgeBases.Select(kb => new KnowledgeBaseQuickVm()
            {
                Id = kb.Id,
                CategoryId = kb.CategoryId,
                Title = kb.Title,
                SeoAlias = kb.SeoAlias,
                Description = kb.Description,
            }).ToListAsync();

            return Ok(knowledgeBaseVms);
        }

        [HttpGet("filter")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetKnowledgeBasesPaging([FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = from k in _context.KnowledgeBases
                        join c in _context.Categories on k.CategoryId equals c.Id
                        select new { k, c };
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.k.Title.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(kb => new KnowledgeBaseQuickVm()
                {
                    Id = kb.k.Id,
                    CategoryId = kb.k.CategoryId,
                    Description = kb.k.Description,
                    SeoAlias = kb.k.SeoAlias,
                    Title = kb.k.Title,
                    CategoryName = kb.c.Name
                })
                .ToListAsync();

            var pagination = new Pagination<KnowledgeBaseQuickVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetById(int id)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound(new ApiNotFoundResponse(""));

            var attachments = await _context.Attachments
                .Where(x => x.KnowledgeBaseId == id)
                .Select(x => new AttachmentVm()
                {
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    FileSize = x.FileSize,
                    Id = x.Id,
                    FileType = x.FileType
                }).ToListAsync();
            var knowledgeBaseVm = new KnowledgeBaseVm()
            {
                Id = knowledgeBase.Id,
                CategoryId = knowledgeBase.CategoryId,
                Title = knowledgeBase.Title,
                SeoAlias = knowledgeBase.SeoAlias,
                Description = knowledgeBase.Description,
                Environment = knowledgeBase.Environment,
                Problem = knowledgeBase.Problem,
                StepToReproduce = knowledgeBase.StepToReproduce,
                ErrorMessage = knowledgeBase.ErrorMessage,
                Workaround = knowledgeBase.Workaround,
                Note = knowledgeBase.Note,
                OwnerUserId = knowledgeBase.OwnerUserId,
                Labels = !string.IsNullOrEmpty(knowledgeBase.Labels) ? knowledgeBase.Labels.Split(',') : null,
                CreateDate = knowledgeBase.CreateDate,
                LastModifiedDate = knowledgeBase.LastModifiedDate,
                NumberOfComments = knowledgeBase.NumberOfComments,
                NumberOfVotes = knowledgeBase.NumberOfVotes,
                NumberOfReports = knowledgeBase.NumberOfReports,
            };
            knowledgeBaseVm.Attachments = attachments;

            return Ok(knowledgeBaseVm);
        }

        [HttpPut("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.UPDATE)]
        [ApiValidationFilter]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutKnowledgeBase(int id, [FromForm] KnowledgeBaseCreateRequest request)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound(new ApiNotFoundResponse(""));

            knowledgeBase.CategoryId = request.CategoryId;
            knowledgeBase.Title = request.Title;
            knowledgeBase.SeoAlias = request.SeoAlias;
            knowledgeBase.Description = request.Description;
            knowledgeBase.Environment = request.Environment;
            knowledgeBase.Problem = request.Problem;
            knowledgeBase.StepToReproduce = request.StepToReproduce;
            knowledgeBase.ErrorMessage = request.ErrorMessage;
            knowledgeBase.Workaround = request.Workaround;
            knowledgeBase.Note = request.Note;
            knowledgeBase.Labels = string.Join(',', request.Labels);

            //Process attachment
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var attachment in request.Attachments)
                {
                    var attachmentEntity = await SaveFile(knowledgeBase.Id, attachment);
                    _context.Attachments.Add(attachmentEntity);
                }
            }

            _context.KnowledgeBases.Update(knowledgeBase);

            //Process label
            if (request.Labels?.Length > 0)
            {
                await ProcessLabel(request, knowledgeBase);
            }

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpDelete("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteKnowledgeBase(int id)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.KnowledgeBases.Remove(knowledgeBase);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var knowledgeBasevm = new KnowledgeBaseVm()
                {
                    Id = knowledgeBase.Id,
                    CategoryId = knowledgeBase.CategoryId,
                    Title = knowledgeBase.Title,
                    SeoAlias = knowledgeBase.SeoAlias,
                    Description = knowledgeBase.Description,
                    Environment = knowledgeBase.Environment,
                    Problem = knowledgeBase.Problem,
                    StepToReproduce = knowledgeBase.StepToReproduce,
                    ErrorMessage = knowledgeBase.ErrorMessage,
                    Workaround = knowledgeBase.Workaround,
                    Note = knowledgeBase.Note,
                    OwnerUserId = knowledgeBase.OwnerUserId,
                    Labels = !string.IsNullOrEmpty(knowledgeBase.Labels) ? knowledgeBase.Labels.Split(',') : null,
                    CreateDate = knowledgeBase.CreateDate,
                    LastModifiedDate = knowledgeBase.LastModifiedDate,
                    NumberOfComments = knowledgeBase.NumberOfComments,
                    NumberOfVotes = knowledgeBase.NumberOfVotes,
                    NumberOfReports = knowledgeBase.NumberOfReports,
                };
                return Ok(knowledgeBasevm);
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        private async Task ProcessLabel(KnowledgeBaseCreateRequest request, KnowledgeBase knowledgeBase)
        {
            foreach (var labelText in request.Labels)
            {
                var labelId = TextHelper.ToUnsignString(labelText.ToString());
                var existingLabel = await _context.Labels.FindAsync(labelId);
                if (existingLabel == null)
                {
                    var labelEntity = new Label()
                    {
                        Id = labelId,
                        Name = labelText.ToString()
                    };
                    _context.Labels.Add(labelEntity);
                }
                if (await _context.LabelInKnowledgeBases.FindAsync(labelId, knowledgeBase.Id) == null)
                {
                    _context.LabelInKnowledgeBases.Add(new LabelInKnowledgeBase()
                    {
                        KnowledgeBaseId = knowledgeBase.Id,
                        LabelId = labelId
                    });
                }
            }
        }

        private async Task<Attachment> SaveFile(int knowledegeBaseId, IFormFile file)
        {
            var originalFileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fileName = $"{originalFileName.Substring(0, originalFileName.LastIndexOf('.'))}{Path.GetExtension(originalFileName)}";
            await _storageService.SaveFileAsync(file.OpenReadStream(), fileName);
            var attachmentEntity = new Attachment()
            {
                FileName = fileName,
                FilePath = _storageService.GetFileUrl(fileName),
                FileSize = file.Length,
                FileType = Path.GetExtension(fileName),
                KnowledgeBaseId = knowledegeBaseId,
            };
            return attachmentEntity;
        }
        #endregion

        #region Comments
        [HttpGet("{knowledgeBaseId}/comments")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetComments(int knowledgeBaseId)
        {
            var query = from c in _context.Comments
                        join u in _context.Users
                            on c.OwnerUserId equals u.Id
                        select new { c, u };

            var commentVms = await query.Select(c => new CommentVm()
            {
                Id = c.c.Id,
                Content = c.c.Content,
                CreateDate = c.c.CreateDate,
                KnowledgeBaseId = c.c.KnowledgeBaseId,
                LastModifiedDate = c.c.LastModifiedDate,
                OwnerUserId = c.c.OwnerUserId,
                OwnerName = c.u.FirstName + " " + c.u.LastName,
            }).ToListAsync();

            return Ok(commentVms);
        }

        [HttpGet("{knowledgeBaseId}/comments/filter")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetCommentsPaging(int? knowledgeBaseId, [FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = from c in _context.Comments
                        join u in _context.Users
                            on c.OwnerUserId equals u.Id
                        select new { c, u };
            if (knowledgeBaseId.HasValue)
            {
                query = query.Where(x => x.c.KnowledgeBaseId == knowledgeBaseId.Value);
            }
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.c.Content.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentVm()
                {
                    Id = c.c.Id,
                    Content = c.c.Content,
                    CreateDate = c.c.CreateDate,
                    KnowledgeBaseId = c.c.KnowledgeBaseId,
                    LastModifiedDate = c.c.LastModifiedDate,
                    OwnerUserId = c.c.OwnerUserId,
                    OwnerName = c.u.FirstName + " " + c.u.LastName
                })
                .ToListAsync();

            var pagination = new Pagination<CommentVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetCommentDetail(int knowledgeBaseId, int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound(new ApiNotFoundResponse(""));

            var user = await _context.Users.FindAsync(comment.OwnerUserId);
            var commentVm = new CommentVm()
            {
                Id = comment.Id,
                Content = comment.Content,
                CreateDate = comment.CreateDate,
                LastModifiedDate = comment.LastModifiedDate,
                KnowledgeBaseId = comment.KnowledgeBaseId,
                OwnerUserId = comment.OwnerUserId,
                OwnerName = user.FirstName + " " + user.LastName
            };
            return Ok(commentVm);
        }

        [HttpPost("{knowledgeBaseId}/comments")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.CREATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PostComment(int knowledgeBaseId, [FromBody] CommentCreateRequest request)
        {
            var comment = new Comment()
            {
                Content = request.Content,
                KnowledgeBaseId = request.KnowledgeBaseId,
                OwnerUserId = User.GetUserId()
            };
            _context.Comments.Add(comment);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfComments = knowledgeBase.NumberOfComments.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetCommentDetail), new { id = knowledgeBaseId, commentId = comment.Id }, request);
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpPut("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.UPDATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PutComment(int knowledgeBaseId, int commentId, [FromBody] CommentCreateRequest request)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound(new ApiNotFoundResponse(""));

            if (comment.OwnerUserId != User.GetUserId())
                return Forbid();

            comment.Content = request.Content;

            _context.Comments.Update(comment);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpDelete("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteComment(int knowledgeBaseId, int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.Comments.Remove(comment);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfComments = knowledgeBase.NumberOfComments.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var commentVm = new CommentVm()
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    KnowledgeBaseId = comment.KnowledgeBaseId,
                    OwnerUserId = comment.OwnerUserId,
                    CreateDate = comment.CreateDate,
                    LastModifiedDate = comment.LastModifiedDate,
                };
                return Ok(commentVm);
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }
        #endregion

        #region Votes
        [HttpGet("{knowledgeBaseId}/votes")]
        public async Task<IActionResult> GetVotes(int knowledgeBaseId)
        {
            var votes = _context.Votes.Where(v => v.KnowledgeBaseId == knowledgeBaseId);

            var voteVms = await votes.Select(v => new VoteVm()
            {
                KnowledgeBaseId = v.KnowledgeBaseId,
                CreateDate = v.CreateDate,
                LastModifiedDate = v.LastModifiedDate,
                UserId = v.UserId,
            }).ToListAsync();

            return Ok(voteVms);
        }

        [HttpPost("{knowledgeBaseId}/votes")]
        [ApiValidationFilter]
        public async Task<IActionResult> PostVote(int knowledgeBaseId, [FromBody] VoteCreateRequest request)
        {
            var vote = await _context.Votes.FindAsync(knowledgeBaseId, request.UserId);
            if (vote != null)
            {
                return BadRequest(new ApiBadRequestResponse("This user has been voted for this KB"));
            }

            vote = new Vote()
            {
                KnowledgeBaseId = request.KnowledgeBaseId,
                UserId = request.UserId,
            };
            _context.Votes.Add(vote);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfVotes = knowledgeBase.NumberOfVotes.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpDelete("{knowledgeBaseId}/votes/{userId}")]
        public async Task<IActionResult> DeleteVote(int knowledgeBaseId, string userId)
        {
            var vote = await _context.Votes.FindAsync(knowledgeBaseId, userId);
            if (vote == null)
            {
                return NotFound(new ApiNotFoundResponse("This user has not been voted for this KB"));
            }

            _context.Votes.Remove(vote);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfVotes = knowledgeBase.NumberOfVotes.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return Ok();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }
        #endregion

        #region Reports
        [HttpGet("{knowledgeBaseId}/reports")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReports(int knowledgeBaseId)
        {
            var reports = _context.Reports.Where(r => r.KnowledgeBaseId == knowledgeBaseId);

            var reportVms = await reports.Select(r => new ReportVm()
            {
                Id = r.Id,
                KnowledgeBaseId = r.KnowledgeBaseId,
                Content = r.Content,
                CreateDate = r.CreateDate,
                IsProcessed = r.IsProcessed,
                LastModifiedDate = r.LastModifiedDate,
                ReportUserId = r.ReportUserId,
            }).ToListAsync();

            return Ok(reportVms);
        }

        [HttpGet("{knowledgeBaseId}/reports/filter")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReportsPaging(int knowledgeBaseId, [FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.Reports.Where(x => x.KnowledgeBaseId == knowledgeBaseId).AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Content.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1 * pageSize))
                .Take(pageSize)
                .Select(r => new ReportVm()
                {
                    Id = r.Id,
                    KnowledgeBaseId = r.KnowledgeBaseId,
                    Content = r.Content,
                    CreateDate = r.CreateDate,
                    IsProcessed = r.IsProcessed,
                    LastModifiedDate = r.LastModifiedDate,
                    ReportUserId = r.ReportUserId,
                })
                .ToListAsync();

            var pagination = new Pagination<ReportVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReportDetail(int knowledgeBaseId, int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound(new ApiNotFoundResponse(""));

            var reportVm = new ReportVm()
            {
                Id = report.Id,
                KnowledgeBaseId = report.KnowledgeBaseId,
                Content = report.Content,
                CreateDate = report.CreateDate,
                IsProcessed = report.IsProcessed,
                LastModifiedDate = report.LastModifiedDate,
                ReportUserId = report.ReportUserId,
            };
            return Ok(reportVm);
        }

        [HttpPost("{knowledgeBaseId}/reports")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.CREATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PostReport(int knowledgeBaseId, [FromBody] ReportCreateRequest request)
        {
            var report = new Report()
            {
                KnowledgeBaseId = request.KnowledgeBaseId,
                Content = request.Content,
                IsProcessed = false,
                ReportUserId = request.ReportUserId,
            };
            _context.Reports.Add(report);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfReports = knowledgeBase.NumberOfReports.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetReportDetail), new { id = knowledgeBaseId, reportId = report.Id }, request);
            }
            else
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
        }

        [HttpPut("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.UPDATE)]
        [ApiValidationFilter]
        public async Task<IActionResult> PutReport(int knowledgeBaseId, int reportId, [FromBody] ReportCreateRequest request)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound(new ApiNotFoundResponse(""));

            if (report.ReportUserId != User.Identity.Name)
                return Forbid();

            report.Content = request.Content;

            _context.Reports.Update(report);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }

        [HttpDelete("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteReport(int knowledgeBaseId, int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.Reports.Remove(report);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest(new ApiBadRequestResponse(""));
            }
            knowledgeBase.NumberOfReports = knowledgeBase.NumberOfReports.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Ok();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }
        #endregion

        #region Attachments
        [HttpGet("{knowledgeBaseId}/attachments")]
        public async Task<IActionResult> GetAttachments(int knowledgeBaseId)
        {
            var attachments = _context.Attachments.Where(a => a.KnowledgeBaseId == knowledgeBaseId);

            var attachmentVms = await attachments.Select(a => new AttachmentVm()
            {
                Id = a.Id,
                KnowledgeBaseId = a.KnowledgeBaseId,
                CreateDate = a.CreateDate,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                FileType = a.FileType,
                LastModifiedDate = a.LastModifiedDate
            }).ToListAsync();

            return Ok(attachmentVms);
        }

        [HttpDelete("{knowledgeBaseId}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int knowledgeBaseId, int attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return NotFound(new ApiNotFoundResponse(""));

            _context.Attachments.Remove(attachment);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Ok();
            }
            return BadRequest(new ApiBadRequestResponse(""));
        }
        #endregion
    }
}
