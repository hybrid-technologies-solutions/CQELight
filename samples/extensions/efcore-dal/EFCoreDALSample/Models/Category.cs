using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EFCoreDALSample.Models
{
    public class Category : CustomKeyPersistableEntity
    {
        [MaxLength(64), PrimaryKey]
        public string Name { get; set; }

        public IEnumerable<Product> Products { get; set; }
    }
}
