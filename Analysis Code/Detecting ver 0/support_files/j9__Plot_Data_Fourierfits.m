%% plots the fourier series fits, for observed and shuffled data.
%note that shuffled data is prepared in j5, and fits to shuffled data in
%j8.

%% Choose an options:
%1 = RTs relative to target onset
%2 = RTs relative to response onset
%3 = Accuracy relative to target onset
%4 = Likelihood relative to response onset

% testtype=1;
usebin=1; %to do: adapt script to also fit fourier to raw data.

%%
cd([datadir filesep 'ProcessedData' filesep 'GFX'])
%load obs and shuffld data for plots:
load('GFX_Data_inGaits.mat')
load('GFX_Data_inGaits_FourierFits.mat')
%%
for testtype=3%1:4
switch testtype
    case 1
        %test RT relative to target onset:
        dataIN = GFX_TargPosData;
        typeOnset = 'Target';
        typeDV = 'RT';
    case 2
        %%
        %test RT relative to response onset:
        dataIN = GFX_RespPosData;
        typeOnset = 'Response';
        typeDV = 'RT';
        %%
    case 3
        %test ACC relative to target onset.
        
        dataIN = GFX_TargPosData;
        typeOnset = 'Target';
        typeDV = 'Accuracy';
        
    case 4
        % Test response (click) likelihood, relative to resp onset.
        dataIN = GFX_RespPosData;
        typeOnset = 'Response';
        typeDV='Counts';
        
end
%%


cfg=[];
cfg.subjIDs = subjIDs;
cfg.type = typeOnset;
cfg.DV = typeDV;
cfg.datadir= datadir; % for orienting to figures folder
cfg.HeadData= GFX_headY;
cfg.pidx1= pidx1;
cfg.pidx2= pidx2;
cfg.plotlevel = 'GFX'; % plot separate figures per participant
cfg.norm=0; % already z scored, so don't tweak.
cfg.ylims = [-.15 .15]; % if norm =0;
cfg.normtype= 'relative';
%%

%%
figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .75 1]);
nsubs = length(cfg.subjIDs);

for nGaits_toPlot=1:2

% plot_FourierFit(cfg,dataIN);
%%
GFX_headY = cfg.HeadData;
usecols = {[0 .7 0], [.7 0 0], [.7 0 .7]}; % R Gr, Prp


pc=1; % plot counter
pspots = [1,3,5,2,4,6]; %suplot order
psubj= 'GFX'; % print ppid.
% both this and the next use the same figure function:

iLR=3; % use combined data (not sep feet)
gaitfield = {'gc', 'doubgc'};
binfield = {'','_binned'};
    
    legp=[]; % for legend
    ppantData=[];
    plotHead=[];
    
    %which field of datastructure to plot?
    if strcmp(cfg.DV, 'RT')
        usefield = [gaitfield{nGaits_toPlot} binfield{usebin+1} '_rts'];        
        ylabis = 'z(RT)';
    elseif strcmp(cfg.DV, 'Accuracy')
        usefield = [gaitfield{nGaits_toPlot} '_binned_Acc'];
         if ~cfg.norm
            ylabis=  cfg.DV;
        else
            ylabis = [cfg.DV 'norm: ' cfg.normtype];
         end
    elseif strcmp(cfg.DV, 'Counts')
        usefield = [gaitfield{nGaits_toPlot} '_binned_counts'];
        ylabis = [cfg.type ' ' cfg.DV];
    end
    
    %collate data:
    for isub= 1:size(dataIN,1)
        
        ppantData(isub,:)= dataIN(isub,iLR).(usefield);
        plotHead(isub,:) = GFX_headY(isub).(gaitfield{nGaits_toPlot});
    end
    %% if normON , normalize as appropriate
    
    if cfg.norm==1
        pM = nanmean(ppantData,2);
        meanVals= repmat(pM, 1, size(ppantData,2));
        
        
        if strcmp(cfg.normtype, 'absolute')
            data = ppantData - meanVals;
        elseif strcmp(cfg.normtype, 'relative')
            data = ppantData  ./ meanVals;
            data=data-1;
        elseif strcmp(cfg.normtype, 'relchange')
            data = (ppantData  - meanVals) ./ meanVals;
        elseif strcmp(cfg.normtype, 'normchange')
            data = (ppantData  - meanVals) ./ (ppantData + meanVals);
        elseif strcmp(cfg.normtype, 'db')
            data = 10*log10(ppantData  ./ meanVals);
        end
        
        ppantData= data;        
    end
    %% other specs:
    if nGaits_toPlot==1
        
        pidx= cfg.pidx1;
        ftnames= {'LR', 'RL', 'combined'};
    else
        pidx= cfg.pidx2;
        ftnames= {'LRL', 'RLR', 'combined'};
    end
    
    %note that pidx is adjusted to all datapoints, if not using the  bin.
    if usebin==0
        pidx=1:size(ppantData,2);
    end
    
    %x axis:          %approx centre point of the binns.
            mdiff = round(mean(diff(pidx)./2));
            xvec = pidx(1:end-1) + mdiff;
    
    %% extracted fourier fits per shuffled series (calculated in j8_' loaded above)
    
    %Hzspace% loaded above
     fits_Rsquared_obsrvd=GFX_FourierNull.([cfg.type 'Ons_' usefield '_fitsRsq_Obs']);
     fits_Rsquared_shuffCV=GFX_FourierNull.([cfg.type 'Ons_' usefield '_fitsRsq_ShuffCV']);
    
    %% %%%%%%%%%%%%%%%%%%% first row of plot. 
    % Mean data, with errorbars head pos overlayed.
    subplot(3,2,nGaits_toPlot)
    hold on;

    gM = squeeze(mean(ppantData));
    stE = CousineauSEM(ppantData);
    
    % finely sampled bar, each gait "%" point.
    bh=bar(xvec, gM);
    hold on;
    errorbar(xvec, gM, stE, ...
        'color', 'k',...
        'linestyle', 'none',...
        'linew', 2);
    bh.FaceColor = usecols{iLR};
    legp(iLR)= bh;
    
    ylabel(ylabis)
    
   %adjust ylims to capture 2SD*range of data    
   sdrange = max(gM) - min(gM);
   ylim([min(gM)-.5*sdrange max(gM)+1*sdrange])
    %%
    % add head pos
    hold on
    yyaxis right
    stEH= CousineauSEM(plotHead);
    pH= nanmean(plotHead,1);
   sh= shadedErrorBar(1:size(plotHead,2), pH, stEH,'b',1) ; 
   sh.mainLine.LineStyle='-';
    legp = sh.mainLine;
    set(gca,'ytick', []);
    lg=legend(legp, 'Head position (vertical)', 'location', 'NorthEast', 'fontsize', 10);
   %% lengthen yyaxis right so legend doesnt obscure
    ytop = max(pH);
    ylt= get(gca,'ylim');
    sdrange = max(pH) - min(pH);
   ylim([min(pH) 1.2*max(pH)])
   
    %%
    title([psubj '  ' ftnames{iLR} ' N=' num2str(nsubs)], 'interpreter', 'none');
    midp=xvec(ceil(length(xvec)/2));
    set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})   
    xlabel([ cfg.type ' onset as % of gait-cycle ']);%
 
    
    %% %%%%%%%%%%%%%%%%%%% second row of plot. 
    % Mean data, now with fourier fit overlayed. 
    %% also prepare next overlay:
    subplot(3,2,nGaits_toPlot+2);   
    % finely sampled bar, each gait "%" point.
    bh=bar(xvec, gM);
    hold on;
    errorbar(xvec, gM, stE, ...
        'color', 'k',...
        'linestyle', 'none',...
        'linew', 2);
    bh.FaceColor = usecols{iLR};
     sdrange = max(gM) - min(gM);
   ylim([min(gM)-.5*sdrange max(gM)+1*sdrange])
    %%
    % perform fourier fit:
    % fit all periods, from .01 to .
    %              for ifreq -
   
    [f,gof]= fit(xvec',gM',  'fourier1');
    
    % plot:
    hold on;
    %             yyaxis right
    h=plot(f, xvec, gM);%,
    h(2).LineWidth = 2;
    h(2).Color = 'k';
    %%
    %treat max xvec as our full 'period'
    fitperiod = f.w;
    %convert to period per samples.
    % include period and Rsquared
    %treat max xvec as our full 'period'
    Hzapp = xvec(end)/ (2*pi/(f.w));
    legdetails = [sprintf('%.2f', Hzapp) ' Hz_G_C, R^2 = ' sprintf('%.2f', gof.rsquare) ];
    legend(h(2), legdetails, 'fontsize', 15, 'autoupdate', 'off')
    
    ylabel(ylabis)
    set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})
    xlabel([ cfg.type ' onset as % of gait-cycle ']);%
    %%
    %             set(gca,
    
    %
    
    subplot(3,2,nGaits_toPlot+4);
    
    pO=plot(Hzspace, fits_Rsquared_obsrvd, 'k', 'linew', 3);
    title('(forced) Fits per frequency')
    ylabel('R^2');
    xlabel('Frequency (Hz)')
    
    hold on;
    
    %
%     plot(Hzspace, fits_Rsquared_shuffCV(1,:), ':-', 'linew', 2, 'color', [.8 .8 .8] );
    ph=plot(Hzspace, fits_Rsquared_shuffCV(3,:), ':', 'linew', 2, 'color', 'k');
    legend([pO, ph],{'observed', '95% CI shuffled data'})
     set(gca,'fontsize', 15)
    %%
end % nGaits


%%
cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' FourierFits'])

print([psubj ' ' cfg.type ' onset ' cfg.DV ' binned fourierfits'],'-dpng');
end % types