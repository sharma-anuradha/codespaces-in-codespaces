// <copyright file="DictionaryValueStringLengthAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// Specifies the minimum and maximum length of characters that are allowed in a
    ///     dictionary (string) value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class DictionaryValueStringLengthAttribute : ValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryValueStringLengthAttribute"/> class.
        /// </summary>
        /// <param name="maximumLength">The maximum length of a string value.</param>
        public DictionaryValueStringLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        /// <summary>
        /// Gets the maximum length of a string value.
        /// </summary>
        public int MaximumLength { get; }

        /// <summary>
        /// Gets or sets the minimum length of a string value.
        /// </summary>
        public int MinimumLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dictionary is nullable or not.
        /// </summary>
        public bool Nullable { get; set; } = true;

        /// <inheritdoc/>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var errorMessage = $"{validationContext.DisplayName} values must be a string with a " +
                $"minimum length of {MinimumLength} and a maximum length of {MaximumLength}.";

            if (Nullable && value == null)
            {
                return ValidationResult.Success;
            }

            var valueDictionary = (IDictionary)value;
            if (valueDictionary == null)
            {
                return new ValidationResult(errorMessage);
            }

            foreach (var rawValue in valueDictionary.Values)
            {
                if (!(rawValue is string stringValue) || stringValue.Length < MinimumLength || stringValue.Length > MaximumLength)
                {
                    return new ValidationResult(errorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}