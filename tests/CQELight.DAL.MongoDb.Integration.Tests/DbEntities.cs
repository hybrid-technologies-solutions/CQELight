using CQELight.DAL.Attributes;
using CQELight.DAL.Common;
using CQELight.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace CQELight.DAL.MongoDb.Integration.Tests
{
    public static class Global
    {
        internal static bool s_globalInit;
    }
    internal class WebSite : PersistableEntity
    {
        [Index(true), Column("URL"), Required]
        public virtual string Url { get; set; }
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Hyperlink> HyperLinks { get; set; } = new List<Hyperlink>();

        [KeyStorageOf(nameof(AzureLocation))]
        public string AzureCountry { get; set; }
        [KeyStorageOf(nameof(AzureLocation))]
        public string AzureDataCenter { get; set; }
        [ForeignKey]
        public AzureLocation AzureLocation { get; set; }
    }

    [ComposedKey(nameof(Country), nameof(DataCenter))]
    internal class AzureLocation : ComposedKeyPersistableEntity
    {
        public string Country { get; set; }
        public string DataCenter { get; set; }
    }

    [Table("Hyperlinks")]
    internal class Hyperlink : CustomKeyPersistableEntity
    {
        [PrimaryKey("Hyperlink"), MaxLength(1024)]
        public string Value { get; set; }
        [ForeignKey, Required]
        public WebSite WebSite { get; set; }
        [KeyStorageOf(nameof(WebSite))]
        protected Guid WebSite_Id { get; set; }
        public override bool IsKeySet() => !string.IsNullOrWhiteSpace(Value);
        public override object GetKeyValue() => Value;
    }

    internal class User : PersistableEntity
    {
        [Column]
        public virtual string Name { get; set; }
        [Column]
        public virtual string LastName { get; set; }
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }

    internal class Post : PersistableEntity
    {
        [MaxLength(65536), Column, Required]
        public virtual string Content { get; set; }
        [MaxLength(2048), Column("ShortAccess"), Index(true)]
        public virtual string QuickUrl { get; set; }
        [DefaultValue(1), Column]
        public virtual int Version { get; set; }
        [DefaultValue(true), Column]
        public virtual bool Published { get; set; }
        [Column]
        public virtual DateTime? PublicationDate { get; set; }
        public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        [ForeignKey]
        [NotNaviguable(NavigationMode.Update)]
        public User Writer { get; set; }
        [KeyStorageOf(nameof(Writer))]
        protected Guid? Writer_Id { get; set; }
        [ForeignKey(DeleteCascade = true), Required]
        public WebSite WebSite { get; set; }
        [KeyStorageOf(nameof(WebSite))]
        protected Guid WebSiteId { get; set; }
    }

    [ComposedKey(nameof(Post), nameof(Tag))]
    internal class PostTag : ComposedKeyPersistableEntity
    {
        [KeyStorageOf(nameof(Post))]
        protected Guid Post_Id { get; set; }
        [ForeignKey, Required]
        public Post Post { get; protected set; }
        [KeyStorageOf(nameof(Tag))]
        protected Guid Tag_Id { get; set; }
        [ForeignKey, Required]
        public Tag Tag { get; protected set; }

        protected PostTag() { }

        public PostTag(Post post, Tag tag)
        {
            Post = post;
            Tag = tag;
        }
        public override bool IsKeySet() => Post != null && Tag != null;
        public override object GetKeyValue() => new { Post = Post, Tag = Tag };
    }

    internal class Tag : PersistableEntity
    {
        [Index(true)]
        [Column]
        public virtual string Value { get; set; }
        public virtual ICollection<PostTag> Posts { get; set; } = new List<PostTag>();
        public virtual ICollection<Word> Words { get; set; } = new List<Word>();
    }

    internal class Word : IPersistableEntity
    {
        [PrimaryKey]
        public virtual string WordValue { get; set; }
        [KeyStorageOf(nameof(Tag))]
        public virtual Guid Tag_Id { get; set; }
        [ForeignKey, Required]
        public virtual Tag Tag { get; set; }

        public object GetKeyValue() => WordValue;

        public bool IsKeySet() => !string.IsNullOrWhiteSpace(WordValue);
    }

    [ComplexIndex(new[] { nameof(Post), nameof(Owner), nameof(Value) }, false)]
    internal class Comment : PersistableEntity
    {
        [Column]
        public virtual string Value { get; set; }
        [ForeignKey, Required]
        [NotNaviguable(NavigationMode.Update)]
        protected virtual User Owner { get; set; }
        [KeyStorageOf(nameof(Owner))]
        protected virtual Guid Owner_Id { get; set; }
        [ForeignKey, Required]
        protected virtual Post Post { get; set; }
        [KeyStorageOf(nameof(Post))]
        protected virtual Guid Post_Id { get; set; }

        protected Comment() { }
        public Comment(string value, User owner, Post post)
        {
            Value = value;
            Owner = owner;
            Post = post;
        }
    }

    [ComposedKey(nameof(FirstPart), nameof(SecondPart))]
    internal class ComposedKeyEntity : IPersistableEntity
    {
        public string FirstPart { get; set; }
        public string SecondPart { get; set; }

        public object GetKeyValue() => FirstPart + SecondPart;

        public bool IsKeySet()
            => !string.IsNullOrWhiteSpace(FirstPart + SecondPart);
    }

    public class SpecificMongoObject
    {
        public MongoDB.Bson.ObjectId Id { get; set; }
        public string Value { get; set; }
        public int IntValue { get; set; }
    }


}
