using AutoMapper;
using Bookstore.Domain.Entities;
using Bookstore.Application.DTOs;

namespace Bookstore.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Book Mappings
        CreateMap<Book, BookResponseDto>()
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.ISBN != null ? src.ISBN.ToString() : string.Empty))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price != null ? src.Price.Amount : 0m))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price != null ? src.Price.Currency : string.Empty))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

        CreateMap<BookCreateDto, Book>()
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => new Bookstore.Domain.ValueObjects.ISBN(src.ISBN)))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => new Bookstore.Domain.ValueObjects.Money(src.Price, src.Currency)));

        // Category Mappings
        CreateMap<Category, CategoryResponseDto>();
        CreateMap<CategoryCreateDto, Category>();

        // User Mappings
        CreateMap<User, UserResponseDto>();

        // Review Mappings
        CreateMap<Review, ReviewResponseDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty));

        // ShoppingCart Mappings
        CreateMap<ShoppingCart, ShoppingCartResponseDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice != null ? src.TotalPrice.Amount : 0m))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.TotalPrice != null ? src.TotalPrice.Currency : string.Empty))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.GetItemCount()))
            .ForMember(dest => dest.IsEmpty, opt => opt.MapFrom(src => src.IsEmpty));

        CreateMap<ShoppingCartItem, ShoppingCartItemResponseDto>()
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book != null ? src.Book.Title : string.Empty))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.Book != null && src.Book.ISBN != null ? src.Book.ISBN.Value : string.Empty))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice != null ? src.UnitPrice.Amount : 0m))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.UnitPrice != null ? src.UnitPrice.Currency : string.Empty));

        // Order Mappings
        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount != null ? src.TotalAmount.Amount : 0m))
            .ForMember(dest => dest.ShippingFee, opt => opt.MapFrom(src => src.ShippingFee != null ? src.ShippingFee.Amount : 0m))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.TotalAmount != null ? src.TotalAmount.Currency : string.Empty))
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book != null ? src.Book.Title : string.Empty))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.Book != null && src.Book.ISBN != null ? src.Book.ISBN.Value : string.Empty))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice != null ? src.UnitPrice.Amount : 0m))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.UnitPrice != null ? src.UnitPrice.Currency : string.Empty));
    }
}
