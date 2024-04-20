﻿using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeSpace.ViewModels.Contents
{
    public class KnowledgeBaseCreateRequest
    {
        public int? Id { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        public string SeoAlias { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Môi trường")]
        public string Environment { get; set; }

        [Display(Name = "Vấn đề gặp phải")]
        public string Problem { get; set; }

        [Display(Name = "Các bước tái hiện")]
        public string StepToReproduce { get; set; }

        [Display(Name = "Lỗi")]
        public string ErrorMessage { get; set; }

        [Display(Name = "Cách xử lý nhanh")]
        public string Workaround { get; set; }

        [Display(Name = "Giải pháp")]
        public string Note { get; set; }

        [Display(Name = "Nhãn")]
        public string[] Labels { get; set; }

        [Display(Name = "Tệp đính kèm")]
        public List<IFormFile> Attachments { get; set; }
    }
}
