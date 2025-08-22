namespace LibraryLending.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class BookUnavailableException : DomainException
{
    public BookUnavailableException(Guid bookId) 
        : base($"Book with ID {bookId} is not available for loan.") { }
}

public class LoanAlreadyReturnedException : DomainException
{
    public LoanAlreadyReturnedException(Guid loanId) 
        : base($"Loan with ID {loanId} has already been returned.") { }
}

public class NotificationException : DomainException
{
    public NotificationException(string message) : base(message) { }
}

public class PatronEmailAlreadyExistsException : DomainException
{
    public PatronEmailAlreadyExistsException(string email) 
        : base($"A patron with email {email} already exists.") { }
}