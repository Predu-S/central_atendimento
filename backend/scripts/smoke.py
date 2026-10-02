import json, urllib.request, urllib.error, datetime
base = 'http://localhost:5080'
def request(path, data=None, method='GET', expected=200):
    payload = None if data is None else json.dumps(data).encode()
    req = urllib.request.Request(base + path, payload, {'Content-Type':'application/json'}, method=method)
    try:
        with urllib.request.urlopen(req) as r: status, result = r.status, json.load(r)
    except urllib.error.HTTPError as e: status, result = e.code, json.load(e)
    assert status == expected, (path, status, result)
    return result
spec = request('/swagger/v1/swagger.json')
required = {
    '/api/dashboard/resumo': ['get'], '/api/dashboard/series': ['get'], '/api/dashboard/sla-em-risco': ['get'],
    '/api/tickets': ['get', 'post'], '/api/tickets/{id}': ['get', 'put'], '/api/tickets/lote': ['patch'],
    '/api/tickets/{id}/mensagens': ['post'], '/api/sla/regras': ['get', 'put'],
    '/api/sla/indicadores': ['get'], '/api/sla/violacoes': ['get'], '/api/equipe/ranking': ['get'],
    '/api/equipe/carga': ['get'], '/api/clientes': ['get'], '/api/clientes/{id}/tickets': ['get']
}
for path, methods in required.items():
    for method in methods: assert method in spec['paths'].get(path, {}), (path, method, 'ausente no Swagger')
assert request('/api/tickets')['total'] == 240
assert len(request('/api/clientes')) == 30
assert len(request('/api/equipe/carga')) == 10
assert len(request('/api/sla/regras')['regras']) == 4
assert len(request('/api/sla/violacoes')) > 0
for p in ['/api/dashboard/resumo','/api/dashboard/series','/api/dashboard/sla-em-risco','/api/sla/indicadores','/api/equipe/ranking','/api/clientes/1/tickets']:
    request(p)
request('/api/tickets/99999', expected=404)
request('/api/tickets?pagina=0', expected=400)
request('/api/tickets?status=99', expected=400)
ticket = dict(assunto='Falha na emissão de NF-e', descricao='A emissão retorna erro de certificado', clienteId=1, tecnicoId=1, equipeId=None, prioridade='Alta', canal='WhatsApp', status='Aberto')
t = request('/api/tickets', ticket, 'POST', 201)['ticket']; id = t['id']
assert t['equipe'] is not None
request(f'/api/tickets/{id}/mensagens', dict(conteudo='Verificar certificado', tecnicoId=1, notaInterna=True), 'POST', 201)
assert request(f'/api/tickets/{id}')['ticket']['primeiraRespostaEm'] is None
request(f'/api/tickets/{id}/mensagens', dict(conteudo='Olá! Vamos verificar.', tecnicoId=1, notaInterna=False), 'POST', 201)
assert request(f'/api/tickets/{id}')['ticket']['primeiraRespostaEm'] is not None
request('/api/tickets/lote', dict(ids=[id], status='AguardandoCliente'), 'PATCH')
assert request(f'/api/tickets/{id}')['ticket']['sla']['pausado']
request('/api/tickets/lote', dict(ids=[id,99999], status='Fechado'), 'PATCH', 404)
assert request(f'/api/tickets/{id}')['ticket']['status'] == 'AguardandoCliente'
ticket['status']='Resolvido'; ticket['csat']=5
request(f'/api/tickets/{id}', ticket, 'PUT')
assert request(f'/api/tickets/{id}')['ticket']['csat'] == 5
rules = request('/api/sla/regras'); request('/api/sla/regras', rules, 'PUT')
rules['horario']['dias']=[]; request('/api/sla/regras', rules, 'PUT', 400)
req = urllib.request.Request(base+'/api/tickets', headers={'Origin':'http://localhost:5173'})
with urllib.request.urlopen(req) as r: assert r.headers['Access-Control-Allow-Origin']=='http://localhost:5173'
print('Smoke API: endpoints, CRUD, filtros, validação, SLA, lote e CORS OK')
