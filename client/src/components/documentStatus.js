const STATUS_NAMES = ['Processing', 'Completed', 'Failed'];

export function statusName(value) {
    if (typeof value === 'number') return STATUS_NAMES[value] ?? 'Unknown';
    return value ?? 'Unknown';
}