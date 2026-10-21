namespace Worker.Domain;

public record FilterRequest(FilterType FilterType, double BlurRadius = 0.0);