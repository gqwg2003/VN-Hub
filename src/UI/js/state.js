/* ===== Application State ===== */

const state = {
    entries: [],
    activeTab: 'library',
    markedSubTab: 'all',
    detailEntry: null,
    pickTarget: null,
    settings: {},
    searchTimer: null,
    searchQuery: '',
    runningGames: new Set(),
    sortBy: 'title',
    sortDir: 'asc',
    gridSize: 'medium',
    selectedIds: new Set(),
    groups: [],
    activeGroupId: null,
    activeTag: null,
    filterStatus: -1,
    _statsCache: null,
    _tagsCache: null,
    _deletedStack: []
};
