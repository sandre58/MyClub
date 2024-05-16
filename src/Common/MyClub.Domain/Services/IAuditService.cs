// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Domain.Services
{
    public interface IAuditService
    {
        void New(IAuditable auditable);

        void Update(IAuditable auditable);
    }
}
