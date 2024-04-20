﻿using FluentValidation;

namespace KnowledgeSpace.ViewModels.Contents
{
    public class KnowledgeBaseCreateRequestValidator : AbstractValidator<KnowledgeBaseCreateRequest>
    {
        public KnowledgeBaseCreateRequestValidator()
        {
            RuleFor(x => x.CategoryId).NotNull().WithMessage("Category is required");

            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");

            RuleFor(x => x.Problem).NotEmpty().WithMessage("Problem is required");

            RuleFor(x => x.Note).NotEmpty().WithMessage("Note is required");
        }
    }
}
