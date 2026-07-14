import { apiFetch } from '@/lib/api';

export interface MenuAccessDto {
  role: number | string;
  menuItem: string;
  hasAccess: boolean;
}

export const roleNameMap: Record<number | string, string> = {
  1: 'Admin',
  2: 'TriageNurse',
  3: 'GP',
  Admin: 'Admin',
  TriageNurse: 'TriageNurse',
  GP: 'GP',
};

export function hasMenuAccess(
  menuItem: string,
  userRoles: string[],
  menuAccesses: MenuAccessDto[]
): boolean {
  if (!menuAccesses.length || !userRoles.length) return false;
  return userRoles.some(uRole =>
    menuAccesses.some(
      ma =>
        roleNameMap[ma.role] === uRole &&
        ma.menuItem === menuItem &&
        ma.hasAccess
    )
  );
}

export async function fetchMenuAccess(token: string): Promise<MenuAccessDto[]> {
  const res = await apiFetch('/api/menuaccess', { token });
  if (!res.ok) return [];
  return res.json();
}
